#!/bin/bash

# Get the directory of the script
SCRIPT_DIR="$(dirname "$0")"

CONVERTERDIR="$SCRIPT_DIR/PresetConverterProject/bin/Release/net9.0/win-x64/publish"
OUTPUTDIR="$HOME/Projects/Temp"
CONVERTER="PresetConverter" # Assuming it's a self-contained executable

# Default environment
DOTNET_ENVIRONMENT="Production"

# Function to handle directories
is_dir() {
    echo "$1 is a directory ..."
    if [ "$DOTNET_ENVIRONMENT" == "Development" ]; then
        echo "DOTNET_ENVIRONMENT is Development, using verbose logging ..."
        "$CONVERTERDIR/$CONVERTER" -i "$1" -o "$OUTPUTDIR" -l -v
    else
        "$CONVERTERDIR/$CONVERTER" -i "$1" -o "$OUTPUTDIR" -l
    fi
    read -p "Press Enter to continue..."
}

# Function to handle files
is_file() {
    echo "$1 is a file ..."
    if [ "$DOTNET_ENVIRONMENT" == "Development" ]; then
        echo "DOTNET_ENVIRONMENT is Development, using verbose logging ..."
        "$CONVERTERDIR/$CONVERTER" -i "$1" -o "$OUTPUTDIR" -k6 -l -v
    else
        "$CONVERTERDIR/$CONVERTER" -i "$1" -o "$OUTPUTDIR" -k6 -l
    fi
    read -p "Press Enter to continue..."
}

# Process arguments
for arg in "$@"; do
    if [ -d "$arg" ]; then
        is_dir "$arg"
    else
        is_file "$arg"
    fi
done

# Open the output directory
open "$OUTPUTDIR"

exit 0