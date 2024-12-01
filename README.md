# Dotnet Containerization and Garbage Collection 

ℹ️ This example was put together on a Mac using a *nix shell scripts and [homebrew](https://brew.sh) and may not work in other environments.

## Motivation



## Setup

### Install dependencies

```sh
brew install minikube kubectl kustomization k6
```

### Run a local kubernetes cluster

```sh
minikube start
```

*Note: To configure shell to use minikube docker environment, run*

```sh
eval $(minikube -p minikube docker-env)
```

### Build the image

```sh
make build
```

### Verify image is available
```sh
minikube image ls --format table
```

### Setup test environment
```sh
kubectl apply -f ./kubectl/observability
```

### Deploy service and expose endpoint

```sh
make serve_dotnet8 # for dotnet 8
make serve_dotnet9 # for dotnet 9
```

Service is exposed on a random port on localhost. Copy URL and port printed out.

## Run Tests

### Install k6
```sh
brew install k6
```

### Run k6 tests

```sh
k6 run k6/test.js -e MAX_USERS=<user count> -e BASE_URL<url copied above>
```

### Access Grafana
```sh
minikube service grafana
```

Login using default user `admin` with password `admin`. 
Import Dashboard JSON from `./grafana/dotnet-opentelemetry-dashboard.json`.

## Teardown

### Reset shell configuration
```sh
 eval $(minikube docker-env --unset)
```