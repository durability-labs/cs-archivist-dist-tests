# Continuous Tests

 1. [Description](#description)
 2. [Prerequisites](#prerequisites)
 3. [Run tests](#run-tests)
 4. [Analyze logs](#analyze-logs)


## Description

 Continuous Tests were developed to perform long lasting tests in different configurations and topologies. Unlike Distributed Tests, they are running continuously, until we stop them manually. Such approach is very useful to detect the issues which may appear over the time when we may have blocking I/O, unclosed pools/connections and etc.

 Usually, we are running Continuous Tests manually and for automated runs, please refer to the [Tests automation](Automation.md).

 We have two projects in the repository
 - [ArchivistNetDeployer](../ArchivistNetDeployer) - Prepare environment to run the tests
 - [ContinuousTests](../ContinuousTests) - Continuous Tests

 And they are used to prepare environment and run Continuous Tests.


## Prerequisites

 1. Kubernetes cluster, to run the tests
 2. kubeconfig file, to access the cluster
 3. [kubectl](https://kubernetes.io/docs/tasks/tools/) installed, to create resources in the cluster
 4. Optional - [OpenLens](https://github.com/MuhammedKalkan/OpenLens) installed, to browse cluster resources


## Run tests
 1. Create a Pod in the cluster, in the `default` namespace and consider to use your own value for `metadata.name`
    <details>
    <summary>tests-runner.yaml</summary>

    ```yaml
    ---
    apiVersion: v1
    kind: Pod
    metadata:
      name: tests-runner
      namespace: default
      labels:
        name: manual-run
    spec:
      containers:
      - name: runner
        image: mcr.microsoft.com/dotnet/sdk:8.0
        env:
        - name: KUBECONFIG
          value: /opt/kubeconfig.yaml
      #   volumeMounts:
      #   - name: kubeconfig
      #     mountPath: /opt/kubeconfig.yaml
      #     subPath: kubeconfig.yaml
      #   - name: logs
      #     mountPath: /var/log/archivist-dist-tests
        command: ["sleep", "infinity"]
      # volumes:
      #   - name: kubeconfig
      #     secret:
      #       secretName: archivist-dist-tests-app-kubeconfig
      #   - name: logs
      #     hostPath:
      #       path: /var/log/archivist-dist-tests
    ```

    ```shell
    kubectl apply -f tests-runner.yaml
    ```

 2. Copy kubeconfig to the runner Pod using the name you set in the previous step
    ```shell
    kubectl cp ~/.kube/archivist-dist-tests.yaml tests-runner:/opt/kubeconfig.yaml
    ```

 3. Exec into the runner Pod using the name you set in the previous step
    ```shell
    # kubectl
    kubectl exec -it tests-runner -- bash

    # OpenLens
    OpenLens --> Pods --> dist-tests-runner --> "Press on it" --> Pod Shell
    ```

 4. Install required packages
    ```shell
    apt update
    apt install -y tmux vim
    ```

 5. Clone Continuous Tests repository
    ```shell
    tmux

    cd /opt
    git clone https://github.com/durability-labs/cs-archivist-dist-tests.git
    ```

 6. Run `ArchivistNetDeployer`
    ```shell
    # Usually take ~ 10 minutes
    cd cs-archivist-dist-tests/Tools/ArchivistNetDeployer

    # Adjust values
    vi deploy-continuous-testnet.sh

    # Deploy Archivist Netwotk
    export RUNID=$(date +%Y%m%d-%H%M%S)
    bash deploy-continuous-testnet.sh
    ```

 7. Run `ContinuousTests`
    ```shell
    cd ../../Tests/ArchivistContinuousTests
    cp ../../Tools/ArchivistNetDeployer/archivist-deployment.json .

    # Adjust values
    vi run.sh

    # Run tests
    bash run.sh
    ```

 8. [Tmux sessions](https://tmuxcheatsheet.com)
    ```shell
    # Detach
    Ctrl + b --> d

    # List
    tmux ls

    # Attach
    tmux a -t 0
    ```


## Analyze logs

 We should check the logs in the `/opt/cs-archivist-dist-tests/Tests/ArchivistContinuousTests/logs` folder
