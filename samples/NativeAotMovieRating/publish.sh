#!/usr/bin/env bash
set -euo pipefail

dotnet publish -c Release ./NativeAotMovieRating.csproj -o ./bin/native-aot
