<template>
  <section class="page-section rbac-page">
    <v-alert
      v-if="errorMessage"
      type="warning"
      variant="tonal"
      class="mb-3"
    >
      {{ errorMessage }}
    </v-alert>
    <v-alert
      v-if="savedMessage"
      type="success"
      variant="tonal"
      class="mb-3"
    >
      {{ savedMessage }}
    </v-alert>

    <div class="rbac-layout">
      <v-card
        rounded="xl"
        elevation="0"
        class="panel-card selector-card"
      >
        <v-card-text class="d-flex flex-column ga-4">
          <div class="selector-row">
            <v-select
              v-model="selectedRole"
              :items="roleOptions"
              :label="t('rbacEditor.roleLabel')"
              item-title="label"
              item-value="name"
              variant="outlined"
              density="compact"
              hide-details
              class="selector-field"
            />
            <v-tooltip
              :text="t('rbacEditor.editGroupRbac')"
              location="top"
            >
              <template #activator="{ props: tooltipProps }">
                <v-btn
                  v-bind="tooltipProps"
                  icon="mdi-pencil"
                  variant="text"
                  color="primary"
                  :disabled="!selectedRole"
                  :loading="loading && mode === 'group'"
                  @click="editGroupRbac"
                />
              </template>
            </v-tooltip>
          </div>

          <div class="selector-row">
            <v-autocomplete
              v-model="selectedUserId"
              v-model:search="userSearch"
              :items="filteredUsers"
              :filter="filterUsers"
              :label="t('rbacEditor.userLabel')"
              item-title="displayName"
              item-value="userId"
              variant="outlined"
              density="compact"
              hide-details
              clearable
              class="selector-field"
              :disabled="!selectedRole"
              @update:menu="onUserMenuOpen"
            />
            <v-tooltip
              :text="t('rbacEditor.editUserRbac')"
              location="top"
            >
              <template #activator="{ props: tooltipProps }">
                <v-btn
                  v-bind="tooltipProps"
                  icon="mdi-pencil"
                  variant="text"
                  color="primary"
                  :disabled="!selectedUserId"
                  :loading="loading && mode === 'user'"
                  @click="editUserRbac"
                />
              </template>
            </v-tooltip>
          </div>
        </v-card-text>
      </v-card>

      <v-card
        rounded="xl"
        elevation="0"
        class="panel-card tree-card"
      >
        <v-card-text class="d-flex flex-column h-100">
          <div class="tree-toolbar">
            <span class="text-subtitle-1 font-weight-medium">{{ t('rbacEditor.accessControl') }}</span>
            <span
              v-if="editingLabel"
              class="text-body-2 text-medium-emphasis"
            >{{ editingLabel }}</span>
            <v-spacer />
            <v-tooltip
              :text="t('rbacEditor.toggleAll')"
              location="top"
            >
              <template #activator="{ props: tooltipProps }">
                <v-btn
                  v-bind="tooltipProps"
                  :icon="allSelected ? 'mdi-checkbox-marked-outline' : 'mdi-checkbox-blank-outline'"
                  variant="text"
                  color="primary"
                  :disabled="!mode"
                  @click="toggleAll"
                />
              </template>
            </v-tooltip>
            <v-btn
              color="primary"
              :loading="saving"
              :disabled="!mode"
              @click="save"
            >
              {{ t('rbacEditor.save') }}
            </v-btn>
          </div>

          <div
            v-if="mode"
            class="tree-scroll"
          >
            <v-treeview
              v-model="selectedIds"
              :items="treeItems"
              item-value="id"
              selectable
              select-strategy="classic"
              open-all
              density="compact"
            />
          </div>

          <div
            v-else
            class="tree-placeholder"
          >
            <v-icon
              icon="mdi-shield-key-outline"
              size="40"
              class="mb-2"
            />
            <p class="text-body-2 text-medium-emphasis">
              {{ t('rbacEditor.placeholderHint') }}
            </p>
          </div>
        </v-card-text>
      </v-card>
    </div>
  </section>
</template>

<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { buildLegacyMenuItems, hasChildren, type MenuItem } from '@/components/layout/menuHelper'
import { getAdminUsers } from '@/services/admin'
import { getGroupRbac, getUserRbac, saveGroupRbac, saveUserRbac } from '@/services/rbac'
import { useSessionStore } from '@/stores/session'
import type { AdminUser } from '@/types/api'

type RbacTreeNode = {
  id: string
  title: string
  children?: RbacTreeNode[]
}

type EditMode = 'group' | 'user'

const { t } = useI18n({ useScope: 'global' })
const sessionStore = useSessionStore()

const loading = ref(false)
const saving = ref(false)
const errorMessage = ref('')
const savedMessage = ref('')

const users = ref<AdminUser[]>([])
const selectedRole = ref<string | null>(null)
const selectedUserId = ref<string | null>(null)
const userSearch = ref('')

const mode = ref<EditMode | null>(null)
const selectedIds = ref<string[]>([])

const roleOptions = computed(() => {
  const roles = [
    { name: 'Guest', label: t('rbacEditor.roles.guest') },
    { name: 'Operator', label: t('rbacEditor.roles.operator') },
    { name: 'Supervisor', label: t('rbacEditor.roles.supervisor') },
    { name: 'Manager', label: t('rbacEditor.roles.manager') },
    { name: 'Admin', label: t('rbacEditor.roles.admin') },
  ]

  return roles.sort((a, b) => a.label.localeCompare(b.label))
})

const filteredUsers = computed(() => {
  if (!selectedRole.value) {
    return []
  }

  const normalizedRole = selectedRole.value.toLowerCase().trim()
  return users.value
    .filter((user) => user.role.toLowerCase().trim() === normalizedRole)
    .sort((a, b) => a.displayName.localeCompare(b.displayName))
})

const editingLabel = computed(() => {
  if (mode.value === 'group' && selectedRole.value) {
    const role = roleOptions.value.find((option) => option.name === selectedRole.value)
    return t('rbacEditor.editingGroup', { role: role?.label ?? selectedRole.value })
  }

  if (mode.value === 'user' && selectedUserId.value) {
    const user = users.value.find((candidate) => candidate.userId === selectedUserId.value)
    return t('rbacEditor.editingUser', { user: user?.displayName ?? '' })
  }

  return ''
})

const treeItems = computed<RbacTreeNode[]>(() => {
  const topLevelItems: MenuItem[] = [
    { title: t('routes.dashboard'), to: '/dashboard', icon: 'mdi-view-dashboard-outline' },
    { title: t('routes.dashboardOperator'), to: '/dashboard/operator', icon: 'mdi-view-dashboard-outline' },
  ]

  const legacyItems = buildLegacyMenuItems(t, sessionStore.profile?.role)

  return buildTreeNodes([...topLevelItems, ...legacyItems], 0)
})

const allLeafIds = computed(() => {
  const ids = new Set<string>()
  collectLeafIds({ id: '', title: '', children: treeItems.value }, ids)
  return [...ids]
})

const allSelected = computed(() =>
  allLeafIds.value.length > 0 && allLeafIds.value.every((id) => selectedIds.value.includes(id)))

onMounted(async () => {
  try {
    const initialRole = selectedRole.value
    if (initialRole) {
      await loadUsersForRole(initialRole)
    }
  } catch {
    errorMessage.value = t('rbacEditor.messages.loadUsersFailed')
  }
})

watch(selectedRole, async (role) => {
  selectedUserId.value = null
  resetPane()

  if (!role) {
    users.value = []
    return
  }

  await loadUsersForRole(role)
})

async function loadUsersForRole(roleName: string) {
  loading.value = true
  errorMessage.value = ''
  userSearch.value = ''

  try {
    users.value = await getAdminUsers({ role: roleName, take: 1000 })
  } catch {
    errorMessage.value = t('rbacEditor.messages.loadUsersFailed')
    users.value = []
  } finally {
    loading.value = false
  }
}

watch(selectedUserId, () => {
  resetPane()
})

function resetPane() {
  mode.value = null
  selectedIds.value = []
}

function filterUsers(item: AdminUser, query: string): boolean {
  const normalized = query.toLowerCase().trim()

  return (
    item.displayName.toLowerCase().includes(normalized) ||
    item.username.toLowerCase().includes(normalized)
  )
}

function onUserMenuOpen(open: boolean) {
  if (open) {
    userSearch.value = ''
  }
}

async function editGroupRbac() {
  if (!selectedRole.value) {
    errorMessage.value = t('rbacEditor.messages.selectRoleFirst')
    return
  }

  loading.value = true
  errorMessage.value = ''
  savedMessage.value = ''

  try {
    const response = await getGroupRbac(selectedRole.value)
    mode.value = 'group'
    selectedIds.value = applyStoredValues(treeItems.value, response.values)
  } catch {
    errorMessage.value = t('rbacEditor.messages.loadFailed')
  } finally {
    loading.value = false
  }
}

async function editUserRbac() {
  if (!selectedUserId.value) {
    errorMessage.value = t('rbacEditor.messages.selectUserFirst')
    return
  }

  loading.value = true
  errorMessage.value = ''
  savedMessage.value = ''

  try {
    const response = await getUserRbac(selectedUserId.value)
    mode.value = 'user'
    selectedIds.value = applyStoredValues(treeItems.value, response.values)
  } catch {
    errorMessage.value = t('rbacEditor.messages.loadFailed')
  } finally {
    loading.value = false
  }
}

function toggleAll() {
  selectedIds.value = allSelected.value ? [] : [...allLeafIds.value]
}

async function save() {
  if (!mode.value) {
    return
  }

  const values: Record<string, boolean> = {}
  collectValues(treeItems.value, new Set(selectedIds.value), values)

  saving.value = true
  errorMessage.value = ''
  savedMessage.value = ''

  try {
    if (mode.value === 'group' && selectedRole.value) {
      await saveGroupRbac(selectedRole.value, values)
    } else if (mode.value === 'user' && selectedUserId.value) {
      await saveUserRbac(selectedUserId.value, values)
    }
    savedMessage.value = t('rbacEditor.messages.saveSuccess')
  } catch {
    errorMessage.value = t('rbacEditor.messages.saveFailed')
  } finally {
    saving.value = false
  }
}

function buildTreeNodes(items: MenuItem[], depth: number): RbacTreeNode[] {
  return items.map((item) => {
    if (hasChildren(item)) {
      const children = buildTreeNodes(item.children!, depth + 1)
      return { id: deriveGroupId(children, depth), title: item.title, children }
    }

    return { id: item.to ?? deriveFallbackId(item.title), title: item.title }
  })
}

function deriveGroupId(children: RbacTreeNode[], depth: number): string {
  const firstLeafId = findFirstLeafId(children)
  const segments = (firstLeafId ?? '').split('/').filter(Boolean)
  return '/' + segments.slice(0, depth + 1).join('/')
}

function deriveFallbackId(title: string): string {
  return `node:${title}`
}

function findFirstLeafId(nodes: RbacTreeNode[]): string | undefined {
  for (const node of nodes) {
    if (!node.children?.length) {
      return node.id
    }

    const found = findFirstLeafId(node.children)
    if (found) {
      return found
    }
  }

  return undefined
}

function applyStoredValues(nodes: RbacTreeNode[], values: Record<string, boolean>): string[] {
  const leafIds = new Set<string>()

  const walk = (nodesToWalk: RbacTreeNode[]) => {
    for (const node of nodesToWalk) {
      if (values[node.id] === true) {
        collectLeafIds(node, leafIds)
      }

      if (node.children?.length) {
        walk(node.children)
      }
    }
  }

  walk(nodes)
  return [...leafIds]
}

function collectValues(nodes: RbacTreeNode[], selected: Set<string>, out: Record<string, boolean>): boolean {
  let allChecked = true

  for (const node of nodes) {
    if (node.children?.length) {
      const childrenAllChecked = collectValues(node.children, selected, out)
      out[node.id] = childrenAllChecked

      if (!childrenAllChecked) {
        allChecked = false
      }
    } else {
      const checked = selected.has(node.id)
      out[node.id] = checked

      if (!checked) {
        allChecked = false
      }
    }
  }

  return allChecked
}

function collectLeafIds(node: RbacTreeNode, ids: Set<string>) {
  if (!node.children?.length) {
    ids.add(node.id)
    return
  }

  for (const child of node.children) {
    collectLeafIds(child, ids)
  }
}
</script>

<style scoped>
.rbac-page {
  min-height: 0;
}

.rbac-layout {
  display: grid;
  grid-template-columns: minmax(280px, 360px) 1fr;
  gap: 1.5rem;
  align-items: stretch;
  height: calc(100vh - 120px);
  min-height: 420px;
}

.selector-card,
.tree-card {
  display: flex;
  flex-direction: column;
  overflow: hidden;
  border: 1px solid rgba(var(--v-theme-primary), 0.15);
}

.selector-card :deep(.v-card-text),
.tree-card :deep(.v-card-text) {
  flex: 1 1 auto;
  min-height: 0;
}

.selector-row {
  display: flex;
  align-items: center;
  gap: 8px;
}

.selector-field {
  flex: 1;
}

.tree-toolbar {
  display: flex;
  align-items: center;
  gap: 8px;
  padding-bottom: 8px;
  border-bottom: 1px solid rgba(var(--v-theme-outline), 0.15);
}

.tree-scroll {
  flex: 1;
  min-height: 0;
  overflow-y: auto;
}

.tree-placeholder {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 4px;
  padding: 48px 16px;
  text-align: center;
  color: rgba(var(--v-theme-on-surface), 0.55);
}

@media (max-width: 960px) {
  .rbac-layout {
    grid-template-columns: 1fr;
    height: auto;
  }

  .tree-scroll {
    max-height: 60vh;
  }
}
</style>
