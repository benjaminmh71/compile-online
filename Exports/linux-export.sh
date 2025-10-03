#!/bin/sh
echo -ne '\033c\033]0;Compile Online\a'
base_path="$(dirname "$(realpath "$0")")"
"$base_path/linux-export.x86_64" "$@"
