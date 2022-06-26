<!-- markdownlint-disable MD033 -->
# Как добавить новую среду

1. [Скриптом](../scripts/create-new-environment.ps1) создать k8s namespace, сервисную учетку с правами на namespace, скрафтить kubeconfig для namespace

    ( :exclamation: Название среды везде должно быть в lowercase)

2. Создать kustomize overlay для среды

    `cp -r manifests/dev manifests/new-env`

    (cкопировать с dev, внутри поменять всё специфичное для среды ручками)

3. Создать Deployment workflow для среды

    `cp .github/workflows/deploy_dev.yml .github/workflows/deploy_new-env.yml`

    (cкопировать с dev, внутри поменять всё специфичное для среды ручками)

4. Создать Environment в проекте GitHub (Settings -> Environments -> New  environment)

5. В GitHub Environment создать секрет с именем `KUBECONFIG`, поместить туда содержимое файла `kubeconfig` (github-actions-new-env) из П.1

6. По аналогии с другими средами, заполнить секреты-настройки в GitHub Environment (секреты с префиксом `KUBESECRET_`)
