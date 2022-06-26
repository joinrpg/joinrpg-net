<#
.Description
Creates new environment in cluster and produces kubeconfig with permissions to it.
#> 

[CmdletBinding()]
param(
    [Parameter(Position = 0, Mandatory = $true)]
    [String]$namespace
)



$CONTEXT=kubectl config current-context
$SERVICE_ACCOUNT_NAME = "github-actions-$NAMESPACE"

$NEW_CONTEXT="github-actions-$NAMESPACE"
$KUBECONFIG_FILE="github-actions-$NAMESPACE"

kubectl create namespace $NAMESPACE --dry-run=client -o yaml | kubectl apply -f -

@"
apiVersion: v1
kind: ServiceAccount
metadata:
  name: github-actions-$namespace
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
"@ >create-namespace.tmp.yml

kubectl apply -f create-namespace.tmp.yml

Remove-Item create-namespace.tmp.yml

#Get token of the ServiceAccount
$SECRET_NAME=kubectl get serviceaccount ${SERVICE_ACCOUNT_NAME} --context ${CONTEXT} --namespace ${NAMESPACE} -o jsonpath='{.secrets[0].name}' | Out-String -NoNewline

Write-Host "Secret name will be $SECRET_NAME"

$TOKEN_DATA=kubectl get secret ${SECRET_NAME} --context ${CONTEXT} --namespace ${NAMESPACE} -o jsonpath='{.data.token}' | Out-String -NoNewline

$TOKEN = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($TOKEN_DATA))

# Craft kubeconfig which uses ServiceAccount created above

# Create a full copy
kubectl config view --raw > "$KUBECONFIG_FILE.full.tmp"
# Switch working context to correct context
kubectl --kubeconfig "$KUBECONFIG_FILE.full.tmp" config use-context ${CONTEXT}
# Minify
kubectl --kubeconfig "$KUBECONFIG_FILE.full.tmp" config view --flatten --minify > "${KUBECONFIG_FILE}.tmp"
# Rename context
kubectl config --kubeconfig "${KUBECONFIG_FILE}.tmp" rename-context ${CONTEXT} ${NEW_CONTEXT}
# Create token user
kubectl config --kubeconfig "${KUBECONFIG_FILE}.tmp" set-credentials ${CONTEXT}-${NAMESPACE}-token-user --token ${TOKEN}
# Set context to use token user
kubectl config --kubeconfig "${KUBECONFIG_FILE}.tmp" set-context ${NEW_CONTEXT} --user ${CONTEXT}-${NAMESPACE}-token-user
# Set context to correct namespace
kubectl config --kubeconfig "${KUBECONFIG_FILE}.tmp" set-context ${NEW_CONTEXT} --namespace ${NAMESPACE}
# Flatten/minify kubeconfig
kubectl config --kubeconfig "${KUBECONFIG_FILE}.tmp" view --flatten --minify > ${KUBECONFIG_FILE}
# Remove tmp
Remove-Item "${KUBECONFIG_FILE}.full.tmp" -ErrorAction Ignore
Remove-Item "${KUBECONFIG_FILE}.tmp" -ErrorAction Ignore
