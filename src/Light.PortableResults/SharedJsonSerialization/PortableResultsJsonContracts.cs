using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Light.PortableResults.SharedJsonSerialization;

/// <summary>
/// <para>
/// Provides the JSON contract resolution that Light.PortableResults uses instead of the reflection-based
/// <see cref="JsonSerializer" /> overloads. All members are safe to call from trimmed and Native AOT applications.
/// </para>
/// <para>
/// Consumers only need to register their own result value types with a <see cref="JsonSerializerContext" />.
/// The envelope and payload types owned by Light.PortableResults are resolved by this class: it prefers a contract
/// supplied by the configured <see cref="JsonSerializerOptions.TypeInfoResolver" /> and otherwise creates a contract
/// from the converter that the options select for the type. Created contracts are cached per
/// <see cref="JsonSerializerOptions" /> instance and can be created after the options became read-only. Creating one
/// makes the options read-only, so that the cached contract cannot be invalidated by a converter registered later.
/// </para>
/// </summary>
public static class PortableResultsJsonContracts
{
    private static readonly ConditionalWeakTable<JsonSerializerOptions, LibraryContracts> ContractsPerOptions = new ();

    /// <summary>
    /// Creates the message of the <see cref="InvalidOperationException" /> that is thrown when the configured
    /// <see cref="JsonSerializerOptions" /> cannot supply serialization metadata for a consumer-owned type.
    /// </summary>
    /// <param name="type">The type whose metadata could not be resolved.</param>
    /// <returns>The exception message naming <paramref name="type" /> and the required remedy.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="type" /> is <see langword="null" />.</exception>
    public static string CreateMissingTypeInfoMessage(Type type)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        return
            $"No JSON serialization metadata was found for type '{type}'. Register this type with the " +
            "JsonSerializerContext that is supplied to the JsonSerializerOptions used by Light.PortableResults.";
    }

    /// <summary>
    /// Creates the message of the <see cref="InvalidOperationException" /> that is thrown when neither a contract nor
    /// a converter can be resolved for a type owned by Light.PortableResults.
    /// </summary>
    /// <param name="type">The Light.PortableResults type whose contract could not be resolved.</param>
    /// <returns>The exception message naming <paramref name="type" /> and the required remedy.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="type" /> is <see langword="null" />.</exception>
    public static string CreateMissingLibraryContractMessage(Type type)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        return
            $"No JSON serialization metadata or converter was found for the Light.PortableResults type '{type}'. " +
            "Call the corresponding AddDefaultPortableResults... extension method on your JsonSerializerOptions.";
    }

    /// <summary>
    /// Materializes the reflection-based type info resolver when the specified options have no resolver and
    /// reflection-based serialization is enabled. This keeps the behavior of the shared default options - which carry
    /// converters but no explicit resolver - unchanged, and is a no-op when reflection is disabled.
    /// </summary>
    /// <param name="options">The serializer options to inspect.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options" /> is <see langword="null" />.</exception>
    public static void EnsureTypeInfoResolver(JsonSerializerOptions options)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        if (options.TypeInfoResolver is not null || !JsonSerializer.IsReflectionEnabledByDefault)
        {
            return;
        }

        PopulateReflectionTypeInfoResolver(options);
    }

    /// <summary>
    /// Resolves the contract for a consumer-owned type, e.g. the value type of a <see cref="Result{T}" />.
    /// </summary>
    /// <typeparam name="T">The consumer-owned type.</typeparam>
    /// <param name="options">The serializer options that must supply the contract.</param>
    /// <returns>The resolved contract.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="options" /> cannot supply serialization metadata for <typeparamref name="T" />.
    /// </exception>
    public static JsonTypeInfo<T> GetRequiredTypeInfo<T>(JsonSerializerOptions options)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        EnsureTypeInfoResolver(options);
        if (options.TryGetTypeInfo(typeof(T), out var typeInfo) && typeInfo is JsonTypeInfo<T> typedTypeInfo)
        {
            return typedTypeInfo;
        }

        throw new InvalidOperationException(CreateMissingTypeInfoMessage(typeof(T)));
    }

    /// <summary>
    /// Resolves the contract for a type owned by Light.PortableResults. A contract supplied by the configured
    /// resolver takes precedence; otherwise a contract is created from the converter that <paramref name="options" />
    /// select for <typeparamref name="TLibraryType" />, falling back to <paramref name="createLibraryConverter" />
    /// when no converter is registered.
    /// </summary>
    /// <typeparam name="TLibraryType">The Light.PortableResults type to resolve.</typeparam>
    /// <param name="options">The serializer options.</param>
    /// <param name="createLibraryConverter">
    /// An optional factory for the library-owned converter that is used when <paramref name="options" /> have no
    /// converter for <typeparamref name="TLibraryType" />.
    /// </param>
    /// <returns>The resolved contract.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when neither a contract nor a converter can be resolved for <typeparamref name="TLibraryType" />.
    /// </exception>
    public static JsonTypeInfo<TLibraryType> GetLibraryTypeInfo<TLibraryType>(
        JsonSerializerOptions options,
        Func<JsonConverter<TLibraryType>>? createLibraryConverter = null
    )
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        if (TryGetCachedTypeInfo<TLibraryType>(options, out var cachedTypeInfo))
        {
            return cachedTypeInfo;
        }

        EnsureTypeInfoResolver(options);
        if (options.TryGetTypeInfo(typeof(TLibraryType), out var resolvedTypeInfo) &&
            resolvedTypeInfo is JsonTypeInfo<TLibraryType> typedTypeInfo)
        {
            return typedTypeInfo;
        }

        var converter = ResolveLibraryConverter(options, createLibraryConverter);
        FreezeBeforeCaching(options);
        var createdTypeInfo = JsonMetadataServices.CreateValueInfo<TLibraryType>(options, converter);
        return CacheTypeInfo(options, createdTypeInfo);
    }

    /// <summary>
    /// Writes a value of a type owned by Light.PortableResults. A contract supplied by the configured resolver takes
    /// precedence; otherwise the converter that <paramref name="options" /> select for
    /// <typeparamref name="TLibraryType" /> is invoked directly, falling back to
    /// <paramref name="createLibraryConverter" /> when no converter is registered. The writer is flushed once the
    /// value has been written, regardless of which of the two paths was taken.
    /// </summary>
    /// <typeparam name="TLibraryType">The Light.PortableResults type to write.</typeparam>
    /// <param name="writer">The JSON writer receiving the value.</param>
    /// <param name="value">The value to write.</param>
    /// <param name="options">The serializer options.</param>
    /// <param name="createLibraryConverter">
    /// An optional factory for the library-owned converter that is used when <paramref name="options" /> have no
    /// converter for <typeparamref name="TLibraryType" />.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="writer" /> or <paramref name="options" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when neither a contract nor a converter can be resolved for <typeparamref name="TLibraryType" />.
    /// </exception>
    public static void WriteLibraryValue<TLibraryType>(
        Utf8JsonWriter writer,
        TLibraryType value,
        JsonSerializerOptions options,
        Func<JsonConverter<TLibraryType>>? createLibraryConverter = null
    )
    {
        if (writer is null)
        {
            throw new ArgumentNullException(nameof(writer));
        }

        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        if (TryGetCachedConverter<TLibraryType>(options, out var cachedConverter))
        {
            WriteWithConverter(writer, value, options, cachedConverter);
            return;
        }

        EnsureTypeInfoResolver(options);
        if (options.TryGetTypeInfo(typeof(TLibraryType), out var resolvedTypeInfo) &&
            resolvedTypeInfo is JsonTypeInfo<TLibraryType> typedTypeInfo)
        {
            JsonSerializer.Serialize(writer, value, typedTypeInfo);
            return;
        }

        var resolvedConverter = ResolveLibraryConverter(options, createLibraryConverter);
        FreezeBeforeCaching(options);
        WriteWithConverter(writer, value, options, CacheConverter(options, resolvedConverter));
    }

    // JsonSerializer.Serialize(Utf8JsonWriter, ...) flushes the writer once it has written the value, while a
    // converter invoked directly does not. Flushing here keeps the observable behavior of the public write APIs
    // independent of whether the contract came from the configured resolver or from a library-owned converter.
    private static void WriteWithConverter<TLibraryType>(
        Utf8JsonWriter writer,
        TLibraryType value,
        JsonSerializerOptions options,
        JsonConverter<TLibraryType> converter
    )
    {
        converter.Write(writer, value, options);
        writer.Flush();
    }

    // JsonSerializer.IsReflectionEnabledByDefault is a link-time constant: when reflection-based serialization is
    // disabled - which is the case in trimmed and Native AOT apps that opt out - the call below is removed entirely
    // and the trimmer never has to preserve the reflection-based resolver. Freezing the options here is not an
    // additional behavior change, because the serialization that follows immediately would freeze them anyway.
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:RequiresUnreferencedCode",
        Justification =
            "The call is guarded by JsonSerializer.IsReflectionEnabledByDefault and is unreachable when reflection-based serialization is disabled."
    )]
    [UnconditionalSuppressMessage(
        "AOT",
        "IL3050:RequiresDynamicCode",
        Justification =
            "The call is guarded by JsonSerializer.IsReflectionEnabledByDefault and is unreachable when reflection-based serialization is disabled."
    )]
    private static void PopulateReflectionTypeInfoResolver(JsonSerializerOptions options) =>
        options.MakeReadOnly(populateMissingResolver: true);

    private static JsonConverter<TLibraryType> ResolveLibraryConverter<TLibraryType>(
        JsonSerializerOptions options,
        Func<JsonConverter<TLibraryType>>? createLibraryConverter
    )
    {
        var registeredConverter = FindRegisteredConverter<TLibraryType>(options);
        if (registeredConverter is not null)
        {
            return registeredConverter;
        }

        if (createLibraryConverter is not null)
        {
            return createLibraryConverter();
        }

        throw new InvalidOperationException(CreateMissingLibraryContractMessage(typeof(TLibraryType)));
    }

    // Follows the order in which JsonSerializerOptions search their converter list - the first entry that can convert
    // the type wins - so converters a caller inserted before the Light.PortableResults defaults keep their precedence
    // and the library converter is only reached when no other entry matches.
    //
    // One deliberate difference: an entry whose CanConvert claims the type but that cannot supply a
    // JsonConverter<TLibraryType> is skipped here, whereas JsonSerializerOptions would select it and then throw.
    // Skipping keeps such an entry from making a library-owned type unwritable and unreadable.
    private static JsonConverter<TLibraryType>? FindRegisteredConverter<TLibraryType>(JsonSerializerOptions options)
    {
        var converters = options.Converters;
        for (var i = 0; i < converters.Count; i++)
        {
            var candidate = converters[i];
            if (!candidate.CanConvert(typeof(TLibraryType)))
            {
                continue;
            }

            if (candidate is JsonConverterFactory factory)
            {
                if (factory.CreateConverter(typeof(TLibraryType), options) is JsonConverter<TLibraryType> created)
                {
                    return created;
                }

                continue;
            }

            if (candidate is JsonConverter<TLibraryType> converter)
            {
                return converter;
            }
        }

        return null;
    }

    private static bool TryGetCachedTypeInfo<TLibraryType>(
        JsonSerializerOptions options,
        [NotNullWhen(true)] out JsonTypeInfo<TLibraryType>? typeInfo
    )
    {
        if (ContractsPerOptions.TryGetValue(options, out var contracts) &&
            contracts.TypeInfos.TryGetValue(typeof(TLibraryType), out var cached))
        {
            typeInfo = (JsonTypeInfo<TLibraryType>) cached;
            return true;
        }

        typeInfo = null;
        return false;
    }

    // Only a frozen instance may be cached against: a mutable one can still gain a converter that would change the
    // outcome of the resolution, which would leave the cache serving a stale contract. Freezing here is not an
    // additional behavior change - reading or writing with the resolved contract freezes the options anyway - but it
    // has to happen explicitly, because invoking a converter directly does not freeze them.
    private static void FreezeBeforeCaching(JsonSerializerOptions options) => options.MakeReadOnly();

    private static JsonTypeInfo<TLibraryType> CacheTypeInfo<TLibraryType>(
        JsonSerializerOptions options,
        JsonTypeInfo<TLibraryType> typeInfo
    )
    {
        var contracts = ContractsPerOptions.GetValue(options, static _ => new LibraryContracts());
        return (JsonTypeInfo<TLibraryType>) contracts.TypeInfos.GetOrAdd(typeof(TLibraryType), typeInfo);
    }

    private static bool TryGetCachedConverter<TLibraryType>(
        JsonSerializerOptions options,
        [NotNullWhen(true)] out JsonConverter<TLibraryType>? converter
    )
    {
        if (ContractsPerOptions.TryGetValue(options, out var contracts) &&
            contracts.Converters.TryGetValue(typeof(TLibraryType), out var cached))
        {
            converter = (JsonConverter<TLibraryType>) cached;
            return true;
        }

        converter = null;
        return false;
    }

    private static JsonConverter<TLibraryType> CacheConverter<TLibraryType>(
        JsonSerializerOptions options,
        JsonConverter<TLibraryType> converter
    )
    {
        var contracts = ContractsPerOptions.GetValue(options, static _ => new LibraryContracts());
        return (JsonConverter<TLibraryType>) contracts.Converters.GetOrAdd(typeof(TLibraryType), converter);
    }

    private sealed class LibraryContracts
    {
        public ConcurrentDictionary<Type, JsonTypeInfo> TypeInfos { get; } = new ();
        public ConcurrentDictionary<Type, JsonConverter> Converters { get; } = new ();
    }
}
