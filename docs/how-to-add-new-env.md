<!-- markdownlint-disable MD033 -->
# Как добавить новую среду

1. [Скриптом](../scripts/create-new-environment.ps1) создать k8s namespace и сервисную учетку с правами на namespace

    `./scripts/create-new-environment.ps1 joinrpg-new-env`

    ( :exclamation: Название среды везде должно быть в lowercase. Неймспейс в кластере — `joinrpg-<среда>`, так его вычисляет deploy workflow)

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
