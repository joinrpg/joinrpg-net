<!-- markdownlint-disable MD033 -->
# Как добавить новую среду

1. Скриптом создать k8s namespace, сервисную учетку с правами на namespace, скрафтить kubeconfig для namespace

    ( :exclamation: Название среды везде должно быть в lowercase)

    <details>
      <summary>Click to expand!</summary>

      ```bash
      #!/bin/bash
      set -euo

      # Update these to match your environment
      NAMESPACE="new-env"

      CONTEXT=$(kubectl config current-context)
      SERVICE_ACCOUNT_NAME=github-actions-${NAMESPACE}

      NEW_CONTEXT=github-actions-${NAMESPACE}
      KUBECONFIG_FILE="github-actions-${NAMESPACE}"

      #Namespace
      kubectl create namespace ${NAMESPACE}

      #Create ServiceAccount and Role. Bind role to the SeviceAccount
      cat <<EOF | kubectl apply -f -
      apiVersion: v1
      kind: ServiceAccount
      metadata:
        name: github-actions-${NAMESPACE}
        namespace: ${NAMESPACE}
      ---

      apiVersion: rbac.authorization.k8s.io/v1
      kind: Role
      metadata:
        name: github-actions-${NAMESPACE}
        namespace: ${NAMESPACE}
      rules:
      - apiGroups: ["*"]
        resources: ["*"]
        verbs: ["*"]

      ---
      apiVersion: rbac.authorization.k8s.io/v1
      kind: RoleBinding
      metadata:
        name: github-actions-${NAMESPACE}
        namespace: ${NAMESPACE}
      roleRef:
        apiGroup: rbac.authorization.k8s.io
        kind: Role
        name: github-actions-${NAMESPACE}
      subjects:
      - namespace: ${NAMESPACE}
        kind: ServiceAccount
        name: github-actions-${NAMESPACE}
      EOF

      #Get token of the ServiceAccount
      SECRET_NAME=$(kubectl get serviceaccount ${SERVICE_ACCOUNT_NAME} \
        --context ${CONTEXT} \
        --namespace ${NAMESPACE} \
        -o jsonpath='{.secrets[0].name}')

      TOKEN_DATA=$(kubectl get secret ${SECRET_NAME} \
        --context ${CONTEXT} \
        --namespace ${NAMESPACE} \
        -o jsonpath='{.data.token}')

      TOKEN=$(echo ${TOKEN_DATA} | base64 -d)

      # Craft kubeconfig which uses ServiceAccount created above

      # Create a full copy
      kubectl config view --raw > ${KUBECONFIG_FILE}.full.tmp
      # Switch working context to correct context
      kubectl --kubeconfig ${KUBECONFIG_FILE}.full.tmp config use-context ${CONTEXT}
      # Minify
      kubectl --kubeconfig ${KUBECONFIG_FILE}.full.tmp \
        config view --flatten --minify > ${KUBECONFIG_FILE}.tmp
      # Rename context
      kubectl config --kubeconfig ${KUBECONFIG_FILE}.tmp \
        rename-context ${CONTEXT} ${NEW_CONTEXT}
      # Create token user
      kubectl config --kubeconfig ${KUBECONFIG_FILE}.tmp \
        set-credentials ${CONTEXT}-${NAMESPACE}-token-user \
        --token ${TOKEN}
      # Set context to use token user
      kubectl config --kubeconfig ${KUBECONFIG_FILE}.tmp \
        set-context ${NEW_CONTEXT} --user ${CONTEXT}-${NAMESPACE}-token-user
      # Set context to correct namespace
      kubectl config --kubeconfig ${KUBECONFIG_FILE}.tmp \
        set-context ${NEW_CONTEXT} --namespace ${NAMESPACE}
      # Flatten/minify kubeconfig
      kubectl config --kubeconfig ${KUBECONFIG_FILE}.tmp \
        view --flatten --minify > ${KUBECONFIG_FILE}
      # Remove tmp
      rm ${KUBECONFIG_FILE}.full.tmp
      rm ${KUBECONFIG_FILE}.tmp
      ```

    </details>

2. Создать kustomize overlay для среды

    `cp -r manifests/dev manifests/new-env`

    (cкопировать с dev, внутри поменять всё специфичное для среды ручками)

3. Создать Deployment workflow для среды

    `cp .github/workflows/deploy_dev.yml .github/workflows/deploy_new-env.yml`

    (cкопировать с dev, внутри поменять всё специфичное для среды ручками)

4. Создать Environment в проекте GitHub (Settings -> Environments -> New  environment)

5. В GitHub Environment создать секрет с именем `KUBECONFIG`, поместить туда содержимое файла `kubeconfig` (github-actions-new-env) из П.1

6. По аналогии с другими средами, заполнить секреты-настройки в GitHub Environment (секреты с префиксом `KUBESECRET_`)
