#!/bin/bash

# Variables
archivist_image=$(awk '{print $NF}' <<< $1)
archivist_compose="archivist-node.yaml"

# Check argument
if [[ -z "$1" ]]; then
  echo "Usage: $0 <archivist_image>"
  exit 0
fi

# Check image
if [[ "${archivist_image}" != "durabilitylabs/archivist-node"** ]]; then
  echo "Wrong image name: ${archivist_image}"
  exit 0
else
  echo "Image for deployment: ${archivist_image}"
fi

# Try image
if ! docker pull "${archivist_image}" &> /dev/null; then
  echo "Failed to pull ${archivist_image}"
  exit 0
fi

# Directory
script_dir="$(dirname -- $0)"
cd "${script_dir}"

# Update archivist image
sed -i "s|image:.*|image: ${archivist_image}|" "${archivist_compose}"

# Stop and Start
docker compose down
docker compose up -d

# Status
docker compose ps

