<#
.Description
Creates new environment in cluster and produces kubeconfig with permissions to it.
#> 

[CmdletBinding()]
param(
    [Parameter(Position = 0, Mandatory = $true)]
    [String]$namespace,
    [Parameter(Position = 1, Mandatory = $false)]
    [String]$ServiceAccountName
)



$CONTEXT=kubectl config current-context

$SERVICE_ACCOUNT_NAME = ""

if ($ServiceAccountName.Length -eq 0)
{
  $SERVICE_ACCOUNT_NAME = "github-actions-$NAMESPACE"
}
else 
{
  $SERVICE_ACCOUNT_NAME = $ServiceAccountName
  
}

$NEW_CONTEXT=$SERVICE_ACCOUNT_NAME
$KUBECONFIG_FILE= "$SERVICE_ACCOUNT_NAME.yaml"

kubectl create namespace $NAMESPACE --dry-run=client -o yaml | kubectl apply -f -

@"
apiVersion: v1
kind: ServiceAccount
metadata:
  name: $SERVICE_ACCOUNT_NAME
  namespace: ${NAMESPACE}
---

apiVersion: rbac.authorization.k8s.io/v1
kind: Role
metadata:
  name: $SERVICE_ACCOUNT_NAME
  namespace: ${NAMESPACE}
rules:
- apiGroups: ["*"]
  resources: ["*"]
  verbs: ["*"]

---
apiVersion: v1
kind: Secret
metadata:
  namespace: ${NAMESPACE}
  name: $SERVICE_ACCOUNT_NAME-secret
  annotations:
    kubernetes.io/service-account.name: $SERVICE_ACCOUNT_NAME
type: kubernetes.io/service-account-token
---
apiVersion: rbac.authorization.k8s.io/v1
kind: RoleBinding
metadata:
  name: $SERVICE_ACCOUNT_NAME
  namespace: ${NAMESPACE}
roleRef:
  apiGroup: rbac.authorization.k8s.io
  kind: Role
  name: $SERVICE_ACCOUNT_NAME
subjects:
- namespace: ${NAMESPACE}
  kind: ServiceAccount
  name: $SERVICE_ACCOUNT_NAME
"@ >create-namespace.tmp.yml

kubectl apply -f create-namespace.tmp.yml

Remove-Item create-namespace.tmp.yml

$TOKEN_DATA=kubectl get secret $SERVICE_ACCOUNT_NAME-secret --context ${CONTEXT} --namespace ${NAMESPACE} -o jsonpath='{.data.token}' | Out-String -NoNewline

$TOKEN = [Text.Encoding]::Utf8.GetString([Convert]::FromBase64String($TOKEN_DATA))

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
