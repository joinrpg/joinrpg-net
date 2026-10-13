<!-- markdownlint-disable MD033 -->
# Как добавить новую среду

1. Создать k8s namespace и сервисную учетку с правами на namespace

    ```bash
    NS=joinrpg-new-env
    kubectl create namespace $NS
    kubectl -n $NS create serviceaccount github-actions-$NS
    kubectl -n $NS create rolebinding github-actions-$NS --clusterrole=admin --serviceaccount=$NS:github-actions-$NS
    kubectl -n $NS apply -f - <<EOF
    apiVersion: v1
    kind: Secret
    metadata:
      name: github-actions-$NS-secret
      annotations:
        kubernetes.io/service-account.name: github-actions-$NS
    type: kubernetes.io/service-account-token
    EOF
    ```

    ( :exclamation: Название среды везде должно быть в lowercase. Неймспейс в кластере — `joinrpg-<среда>`, так его вычисляет deploy workflow)

    Образы тянутся из Selectel Container Registry через секрет `craas-auth`, его в неймспейсы кладёт интеграция кластера с Container Registry. Проверить, что секрет появился в новом неймспейсе: `kubectl -n $NS get secret craas-auth`.

2. Создать kustomize overlay для среды

    `cp -r manifests/dev manifests/new-env`

    (cкопировать с dev, внутри поменять всё специфичное для среды ручками)

3. Создать Deployment workflow для среды

    `cp .github/workflows/deploy_dev.yml .github/workflows/deploy_new-env.yml`

    (cкопировать с dev, внутри поменять всё специфичное для среды ручками)

4. Создать Environment в проекте GitHub (Settings -> Environments -> New  environment)

5. В GitHub Environment создать секрет с именем `SELECTEL_DEPLOY_SA_SECRET`, поместить туда YAML объекта Secret сервисной учетки из П.1 (в нём есть и `token`, и `ca.crt`):

    `kubectl -n joinrpg-new-env get secret github-actions-joinrpg-new-env-secret -o yaml`

    Адрес API кластера берётся из переменной репозитория `SELECTEL_K8S_SERVER`, она общая для всех сред.

6. По аналогии с другими средами, заполнить секреты-настройки в GitHub Environment (секреты с префиксом `KUBESECRET_`)
