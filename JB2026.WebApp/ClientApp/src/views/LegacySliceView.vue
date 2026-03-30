<template>
  <section class="page-section">
    <div class="hero-card">
      <div>
        <p class="eyebrow mb-2">{{ t('legacySlice.eyebrow') }}</p>
        <h1 class="text-h4 mb-3">{{ titleText }}</h1>
        <p class="text-body-1 text-medium-emphasis mb-0">
          {{ t('legacySlice.mappedFrom', { folder: folderText }) }}
        </p>
      </div>
      <v-chip :color="slice?.enabled ? 'success' : 'warning'" variant="tonal" size="large">
        {{ slice?.enabled ? t('legacySlice.enabledInSpa') : t('legacySlice.legacyRouteActive') }}
      </v-chip>
    </div>

    <v-card rounded="xl" elevation="0" class="panel-card">
      <v-card-title class="d-flex justify-space-between align-center ga-3">
        <div>
          <h3 class="text-h6 mb-1">{{ t('legacySlice.routeStatusTitle') }}</h3>
          <p class="text-body-2 text-medium-emphasis mb-0">{{ t('legacySlice.routeStatusSubtitle') }}</p>
        </div>
        <v-btn variant="text" color="primary" @click="reload">{{ t('common.refresh') }}</v-btn>
      </v-card-title>
      <v-card-text>
        <v-chip-group v-if="prefixes.length > 0" column>
          <v-chip v-for="prefix in prefixes" :key="prefix" color="secondary" variant="outlined">{{ prefix }}</v-chip>
        </v-chip-group>
        <p v-else class="text-body-2 text-medium-emphasis mb-0">{{ t('legacySlice.noPrefixes') }}</p>
      </v-card-text>
    </v-card>

    <v-card rounded="xl" elevation="0" class="panel-card">
      <v-card-title>
        <h3 class="text-h6 mb-1">{{ t('legacySlice.knownEntryPointsTitle') }}</h3>
        <p class="text-body-2 text-medium-emphasis mb-0">{{ t('legacySlice.knownEntryPointsSubtitle') }}</p>
      </v-card-title>
      <v-card-text>
        <v-table density="comfortable">
          <thead>
            <tr>
              <th class="text-left">{{ t('legacySlice.legacyRoute') }}</th>
              <th class="text-left">{{ t('common.purpose') }}</th>
              <th class="text-left">{{ t('common.action') }}</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="route in sampleRoutes" :key="route.path">
              <td><code>{{ route.path }}</code></td>
              <td>{{ route.description }}</td>
              <td>
                <v-btn size="small" variant="tonal" color="primary" :href="route.path" target="_blank">{{ t('common.open') }}</v-btn>
              </td>
            </tr>
          </tbody>
        </v-table>
      </v-card-text>
    </v-card>

    <v-card rounded="xl" elevation="0" class="panel-card">
      <v-card-title>
        <h3 class="text-h6 mb-1">{{ t('legacySlice.computedHandlingTitle') }}</h3>
        <p class="text-body-2 text-medium-emphasis mb-0">{{ t('legacySlice.computedHandlingSubtitle') }}</p>
      </v-card-title>
      <v-card-text>
        <v-table density="comfortable">
          <thead>
            <tr>
              <th class="text-left">{{ t('legacySlice.legacyRoute') }}</th>
              <th class="text-left">{{ t('legacySlice.handlingMode') }}</th>
              <th class="text-left">{{ t('legacySlice.resolvedTarget') }}</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="route in routeStatuses" :key="route.path">
              <td><code>{{ route.path }}</code></td>
              <td>
                <v-chip size="small" variant="tonal" :color="chipColor(route.handlingMode)">
                  {{ formatHandlingMode(route.handlingMode) }}
                </v-chip>
              </td>
              <td>
                <code v-if="route.resolvedTargetUrl">{{ route.resolvedTargetUrl }}</code>
                <span v-else class="text-medium-emphasis">-</span>
              </td>
            </tr>
          </tbody>
        </v-table>
      </v-card-text>
    </v-card>

    <v-card rounded="xl" elevation="0" class="panel-card">
      <v-card-title>
        <h3 class="text-h6 mb-1">{{ t('legacySlice.readinessTitle') }}</h3>
        <p class="text-body-2 text-medium-emphasis mb-0">{{ t('legacySlice.readinessSubtitle') }}</p>
      </v-card-title>
      <v-card-text v-if="readiness">
        <div class="d-flex flex-wrap ga-2 mb-4">
          <v-chip size="small" variant="tonal" :color="readiness.enabled ? 'success' : 'warning'">
            {{ t('legacySlice.sliceState', { state: readiness.enabled ? t('legacySlice.enabled') : t('legacySlice.disabled') }) }}
          </v-chip>
          <v-chip size="small" variant="tonal" :color="readiness.legacyBaseConfigured ? 'success' : 'warning'">
            {{
              t('legacySlice.legacyBaseState', {
                state: readiness.legacyBaseConfigured ? t('legacySlice.configured') : t('legacySlice.missing'),
              })
            }}
          </v-chip>
          <v-chip size="small" variant="outlined" color="secondary">{{ t('legacySlice.sampleRoutes', { count: readiness.totalSampleRoutes }) }}</v-chip>
        </div>

        <v-table density="comfortable" class="mb-3">
          <thead>
            <tr>
              <th class="text-left">{{ t('common.mode') }}</th>
              <th class="text-left">{{ t('common.count') }}</th>
            </tr>
          </thead>
          <tbody>
            <tr>
              <td>{{ t('legacySlice.modeSpa') }}</td>
              <td>{{ readiness.spaRoutes }}</td>
            </tr>
            <tr>
              <td>{{ t('legacySlice.modeLegacyRedirect') }}</td>
              <td>{{ readiness.legacyRedirectRoutes }}</td>
            </tr>
            <tr>
              <td>{{ t('legacySlice.modeLegacyPlaceholder') }}</td>
              <td>{{ readiness.legacyPlaceholderRoutes }}</td>
            </tr>
            <tr>
              <td>{{ t('legacySlice.modeUnmanaged') }}</td>
              <td>{{ readiness.unmanagedRoutes }}</td>
            </tr>
          </tbody>
        </v-table>

        <p class="text-body-2 font-weight-medium mb-1">{{ t('legacySlice.apiDependencyChecklist') }}</p>
        <v-table density="comfortable" class="mb-3">
          <thead>
            <tr>
              <th class="text-left">{{ t('common.dependency') }}</th>
              <th class="text-left">{{ t('common.contract') }}</th>
              <th class="text-left">{{ t('common.status') }}</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="dependency in readiness.apiDependencies" :key="`${dependency.method}:${dependency.route}`">
              <td>{{ dependency.name }}</td>
              <td><code>{{ dependency.method }} {{ dependency.route }}</code></td>
              <td>
                <v-chip size="small" variant="tonal" :color="dependency.implemented ? 'success' : 'warning'">
                  {{ dependency.implemented ? t('legacySlice.implementationImplemented') : t('legacySlice.implementationPending') }}
                </v-chip>
              </td>
            </tr>
          </tbody>
        </v-table>

        <div v-if="readiness.blockers.length > 0">
          <p class="text-body-2 font-weight-medium mb-1">{{ t('legacySlice.blockers') }}</p>
          <ul class="text-body-2">
            <li v-for="blocker in readiness.blockers" :key="blocker">{{ blocker }}</li>
          </ul>
        </div>
        <p v-else class="text-body-2 text-success mb-0">{{ t('legacySlice.noBlockers') }}</p>
      </v-card-text>
      <v-card-text v-else>
        <p class="text-body-2 text-medium-emphasis mb-0">{{ t('legacySlice.readinessUnavailable') }}</p>
      </v-card-text>
    </v-card>

    <v-card rounded="xl" elevation="0" class="panel-card">
      <v-card-title>
        <h3 class="text-h6 mb-1">{{ t('legacySlice.actionPlanTitle') }}</h3>
        <p class="text-body-2 text-medium-emphasis mb-0">{{ t('legacySlice.actionPlanSubtitle') }}</p>
      </v-card-title>
      <v-card-text v-if="actionPlan && actionPlan.steps.length > 0">
        <ol class="text-body-2 ps-4">
          <li v-for="step in actionPlan.steps" :key="step.order" class="mb-2">
            <strong>{{ step.title }}</strong>
            <div class="text-medium-emphasis">{{ step.details }}</div>
          </li>
        </ol>
      </v-card-text>
      <v-card-text v-else>
        <p class="text-body-2 text-medium-emphasis mb-0">{{ t('legacySlice.actionPlanUnavailable') }}</p>
      </v-card-text>
    </v-card>
  </section>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { getLegacySliceActionPlan, getLegacySliceReadinessSummary, getLegacySliceRouteStatus } from '@/services/legacySlices'
import { useLegacySlicesStore } from '@/stores/legacySlices'
import type {
  LegacyRouteHandlingMode,
  LegacySliceActionPlan,
  LegacySliceReadinessSummary,
  LegacySliceSampleRouteStatus,
} from '@/types/api'

const props = defineProps<{
  sliceKey: string
}>()

const store = useLegacySlicesStore()
const { t } = useI18n({ useScope: 'global' })
const routeStatuses = ref<LegacySliceSampleRouteStatus[]>([])
const readiness = ref<LegacySliceReadinessSummary | null>(null)
const actionPlan = ref<LegacySliceActionPlan | null>(null)

onMounted(async () => {
  await reload()
})

const slice = computed(() => store.getByKey(props.sliceKey))
const titleText = computed(() => slice.value?.displayName ?? props.sliceKey)
const folderText = computed(() => slice.value?.legacyFolder ?? '-')
const prefixes = computed(() => slice.value?.prefixes ?? [])
const sampleRoutes = computed(() => slice.value?.sampleRoutes ?? [])

async function reload() {
  await store.load()
  try {
    const status = await getLegacySliceRouteStatus(props.sliceKey)
    routeStatuses.value = status.routes
  } catch {
    routeStatuses.value = []
  }

  try {
    readiness.value = await getLegacySliceReadinessSummary(props.sliceKey)
  } catch {
    readiness.value = null
  }

  try {
    actionPlan.value = await getLegacySliceActionPlan(props.sliceKey)
  } catch {
    actionPlan.value = null
  }
}

function chipColor(mode: LegacyRouteHandlingMode): string {
  switch (mode) {
    case 'spa':
      return 'success'
    case 'legacy-redirect':
      return 'warning'
    case 'legacy-placeholder':
      return 'orange'
    default:
      return 'secondary'
  }
}

function formatHandlingMode(mode: LegacyRouteHandlingMode): string {
  switch (mode) {
    case 'spa':
      return t('legacySlice.modeSpa')
    case 'legacy-redirect':
      return t('legacySlice.modeLegacyRedirect')
    case 'legacy-placeholder':
      return t('legacySlice.modeLegacyPlaceholder')
    default:
      return t('legacySlice.modeUnmanaged')
  }
}
</script>
