#!/bin/bash

# Variables
## Common
SOURCE="${SOURCE:-https://github.com/durability-labs/cs-archivist-dist-tests.git}"
BRANCH="${BRANCH:-main}"
FOLDER="${FOLDER:-/opt/cs-archivist-dist-tests}"

## Tests specific
DEPLOYMENT_ARCHIVISTNETDEPLOYER_PATH="${DEPLOYMENT_ARCHIVISTNETDEPLOYER_PATH:-Tools/ArchivistNetDeployer}"
DEPLOYMENT_ARCHIVISTNETDEPLOYER_RUNNER="${DEPLOYMENT_ARCHIVISTNETDEPLOYER_RUNNER:-deploy-continuous-testnet.sh}"
CONTINUOUS_TESTS_FOLDER="${CONTINUOUS_TESTS_FOLDER:-Tests/ArchivistContinuousTests}"
CONTINUOUS_TESTS_RUNNER="${CONTINUOUS_TESTS_RUNNER:-run.sh}"

# Get code
echo -e "Cloning ${SOURCE} to ${FOLDER}\n"
git clone -b "${BRANCH}" "${SOURCE}" "${FOLDER}"
echo -e "\nChanging folder to ${FOLDER}\n"
cd "${FOLDER}"

# Run tests
echo -e "Running tests from branch '$(git branch --show-current) ($(git rev-parse --short HEAD))'\n"

if [[ "${TESTS_TYPE}" == "continuous-tests" ]]; then
  echo -e "Running ArchivistNetDeployer\n"
  bash "${DEPLOYMENT_ARCHIVISTNETDEPLOYER_PATH}"/"${DEPLOYMENT_ARCHIVISTNETDEPLOYER_RUNNER}"
  echo
  echo -e "Running continuous-tests\n"
  bash "${CONTINUOUS_TESTS_FOLDER}"/"${CONTINUOUS_TESTS_RUNNER}"
else
  echo -e "Running ${TESTS_TYPE}\n"
  exec "$@"
fi
