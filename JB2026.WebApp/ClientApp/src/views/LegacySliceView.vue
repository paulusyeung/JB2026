<template>
  <section class="page-section">
    <div class="hero-card">
      <div>
        <p class="eyebrow mb-2">Legacy Module</p>
        <h1 class="text-h4 mb-3">{{ titleText }}</h1>
        <p class="text-body-1 text-medium-emphasis mb-0">
          Module mapped from Job.Book folder <strong>{{ folderText }}</strong> for incremental migration planning.
        </p>
      </div>
      <v-chip :color="slice?.enabled ? 'success' : 'warning'" variant="tonal" size="large">
        {{ slice?.enabled ? 'Enabled in SPA' : 'Legacy route active' }}
      </v-chip>
    </div>

    <v-card rounded="xl" elevation="0" class="panel-card">
      <v-card-title class="d-flex justify-space-between align-center ga-3">
        <div>
          <h3 class="text-h6 mb-1">Slice route status</h3>
          <p class="text-body-2 text-medium-emphasis mb-0">Feature flag prefixes currently mapped by the coexistence middleware.</p>
        </div>
        <v-btn variant="text" color="primary" @click="reload">Refresh</v-btn>
      </v-card-title>
      <v-card-text>
        <v-chip-group v-if="prefixes.length > 0" column>
          <v-chip v-for="prefix in prefixes" :key="prefix" color="secondary" variant="outlined">{{ prefix }}</v-chip>
        </v-chip-group>
        <p v-else class="text-body-2 text-medium-emphasis mb-0">No prefixes configured for this slice key yet.</p>
      </v-card-text>
    </v-card>

    <v-card rounded="xl" elevation="0" class="panel-card">
      <v-card-title>
        <h3 class="text-h6 mb-1">Known legacy entry points</h3>
        <p class="text-body-2 text-medium-emphasis mb-0">Representative Job.Book routes used to validate coexistence behavior.</p>
      </v-card-title>
      <v-card-text>
        <v-table density="comfortable">
          <thead>
            <tr>
              <th class="text-left">Legacy route</th>
              <th class="text-left">Purpose</th>
              <th class="text-left">Action</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="route in sampleRoutes" :key="route.path">
              <td><code>{{ route.path }}</code></td>
              <td>{{ route.description }}</td>
              <td>
                <v-btn size="small" variant="tonal" color="primary" :href="route.path" target="_blank">Open</v-btn>
              </td>
            </tr>
          </tbody>
        </v-table>
      </v-card-text>
    </v-card>

    <v-card rounded="xl" elevation="0" class="panel-card">
      <v-card-title>
        <h3 class="text-h6 mb-1">Computed handling</h3>
        <p class="text-body-2 text-medium-emphasis mb-0">Current server-side decision for each legacy route based on feature flags and legacy base URL.</p>
      </v-card-title>
      <v-card-text>
        <v-table density="comfortable">
          <thead>
            <tr>
              <th class="text-left">Legacy route</th>
              <th class="text-left">Handling mode</th>
              <th class="text-left">Resolved target</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="route in routeStatuses" :key="route.path">
              <td><code>{{ route.path }}</code></td>
              <td>
                <v-chip size="small" variant="tonal" :color="chipColor(route.handlingMode)">
                  {{ route.handlingMode }}
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
        <h3 class="text-h6 mb-1">Migration readiness summary</h3>
        <p class="text-body-2 text-medium-emphasis mb-0">Server-calculated readiness and blockers for this slice.</p>
      </v-card-title>
      <v-card-text v-if="readiness">
        <div class="d-flex flex-wrap ga-2 mb-4">
          <v-chip size="small" variant="tonal" :color="readiness.enabled ? 'success' : 'warning'">
            Slice {{ readiness.enabled ? 'enabled' : 'disabled' }}
          </v-chip>
          <v-chip size="small" variant="tonal" :color="readiness.legacyBaseConfigured ? 'success' : 'warning'">
            Legacy base {{ readiness.legacyBaseConfigured ? 'configured' : 'missing' }}
          </v-chip>
          <v-chip size="small" variant="outlined" color="secondary">Sample routes: {{ readiness.totalSampleRoutes }}</v-chip>
        </div>

        <v-table density="comfortable" class="mb-3">
          <thead>
            <tr>
              <th class="text-left">Mode</th>
              <th class="text-left">Count</th>
            </tr>
          </thead>
          <tbody>
            <tr>
              <td>spa</td>
              <td>{{ readiness.spaRoutes }}</td>
            </tr>
            <tr>
              <td>legacy-redirect</td>
              <td>{{ readiness.legacyRedirectRoutes }}</td>
            </tr>
            <tr>
              <td>legacy-placeholder</td>
              <td>{{ readiness.legacyPlaceholderRoutes }}</td>
            </tr>
            <tr>
              <td>unmanaged</td>
              <td>{{ readiness.unmanagedRoutes }}</td>
            </tr>
          </tbody>
        </v-table>

        <p class="text-body-2 font-weight-medium mb-1">API dependency checklist</p>
        <v-table density="comfortable" class="mb-3">
          <thead>
            <tr>
              <th class="text-left">Dependency</th>
              <th class="text-left">Contract</th>
              <th class="text-left">Status</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="dependency in readiness.apiDependencies" :key="`${dependency.method}:${dependency.route}`">
              <td>{{ dependency.name }}</td>
              <td><code>{{ dependency.method }} {{ dependency.route }}</code></td>
              <td>
                <v-chip size="small" variant="tonal" :color="dependency.implemented ? 'success' : 'warning'">
                  {{ dependency.implemented ? 'implemented' : 'pending' }}
                </v-chip>
              </td>
            </tr>
          </tbody>
        </v-table>

        <div v-if="readiness.blockers.length > 0">
          <p class="text-body-2 font-weight-medium mb-1">Blockers</p>
          <ul class="text-body-2">
            <li v-for="blocker in readiness.blockers" :key="blocker">{{ blocker }}</li>
          </ul>
        </div>
        <p v-else class="text-body-2 text-success mb-0">No configuration blockers detected for this slice.</p>
      </v-card-text>
      <v-card-text v-else>
        <p class="text-body-2 text-medium-emphasis mb-0">Readiness summary unavailable.</p>
      </v-card-text>
    </v-card>

    <v-card rounded="xl" elevation="0" class="panel-card">
      <v-card-title>
        <h3 class="text-h6 mb-1">Migration action plan</h3>
        <p class="text-body-2 text-medium-emphasis mb-0">Ordered next steps generated from readiness blockers and dependency status.</p>
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
        <p class="text-body-2 text-medium-emphasis mb-0">Action plan unavailable.</p>
      </v-card-text>
    </v-card>
  </section>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
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
</script>
