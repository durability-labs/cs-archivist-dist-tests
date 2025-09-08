# ArchivistNetDeployer

This CLI tool allows you to create a network of Archivist nodes in a kubernetes cluster. There are many arguments that allow you to configure every aspect of the deployment.  Each argument can also be controlled using an environment variable. I won't copy-paste the list here because then it'll have to be kept up-to-date. Just run:
`dotnet run -- --help`

## Output
After the deployment has successfully finished, a `archivist-deployment.json` file will be created. This file contains all the information necessary to interface with the deployed network. It can be used by other tools, for example the ArchivistContinuousTests runner CLI tool.

## Overrides
The arguments allow you to configure quite a bit, but not everything. Here are some environment variables the ArchivistNetDeployer will respond to. None of these are required.

| Variable         | Description                                                                                                    |
|------------------|----------------------------------------------------------------------------------------------------------------|
| DEPLOYID         | A pod-label 'deployid' is added to each pod created during the tests. Use this to set the value of that label. |
| TESTID           | Similar to RUNID, except the label is 'testid'.                                                                |
| ARCHIVISTDOCKERIMAGE | If set, this will be used instead of the default Archivist docker image.                                           |

## Using a local Archivist repository
If you have a clone of the Archivist git repository, and you want to deploy a network using your local modifications, the following environment variable options are for you. Please note that any changes made in Archivist's 'vendor' directory will be discarded during the build process.

| Variable       | Description                                                                                                              |
|----------------|--------------------------------------------------------------------------------------------------------------------------|
| ARCHIVISTREPOPATH  | Path to the Archivist repository.                                                                                            |
| DOCKERUSERNAME | Username of your Dockerhub account.                                                                                      |
| DOCKERPASSWORD | Password OR access-token for your Dockerhub account. You can omit this variable to use your system-default account.      |
| DOCKERTAG      | Optional. Tag used for docker image that will be built and pushed to the Dockerhub account. Random ID used when not set. |
