<script setup lang="ts">
import { computed, onMounted, reactive } from 'vue'
import type { JobDetail, JobListItem, TokenResponse } from './types'

const state = reactive({
  apiBaseUrl: 'http://localhost:5188',
  startOn: '2026-03-27',
  days: 10,
  role: 'Manager',
  token: '',
  jobs: [] as JobListItem[],
  selectedJob: null as JobDetail | null,
  isLoading: false,
  error: '',
})

const selectedSummary = computed(() => state.selectedJob?.orderTitle ?? 'Select a job')

onMounted(async () => {
  await refreshData()
})

async function refreshData() {
  state.isLoading = true
  state.error = ''

  try {
    if (!state.token) {
      state.token = await fetchToken()
    }

    const response = await fetch(`${state.apiBaseUrl}/api/v1/jobs/range?startOn=${state.startOn}&days=${state.days}`, {
      headers: {
        Authorization: `Bearer ${state.token}`,
      },
    })

    if (!response.ok) {
      throw new Error(`Jobs request failed with ${response.status}`)
    }

    state.jobs = await response.json() as JobListItem[]

    if (state.jobs.length > 0) {
      await selectJob(state.jobs[0].orderId)
    } else {
      state.selectedJob = null
    }
  } catch (error) {
    state.error = error instanceof Error ? error.message : 'Unexpected error loading jobs.'
  } finally {
    state.isLoading = false
  }
}

async function selectJob(orderId: string) {
  const response = await fetch(`${state.apiBaseUrl}/api/v1/jobs/${orderId}`, {
    headers: {
      Authorization: `Bearer ${state.token}`,
    },
  })

  if (!response.ok) {
    throw new Error(`Job detail request failed with ${response.status}`)
  }

  state.selectedJob = await response.json() as JobDetail
}

async function fetchToken() {
  const response = await fetch(`${state.apiBaseUrl}/api/v1/auth/token`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({
      displayName: 'Vue 3 spike operator',
      role: state.role,
    }),
  })

  if (!response.ok) {
    throw new Error(`Token request failed with ${response.status}`)
  }

  const payload = await response.json() as TokenResponse
  return payload.accessToken
}
</script>

<template>
  <div class="shell">
    <section class="hero">
      <div>
        <p class="eyebrow">Phase 2 UI Spike</p>
        <h1>Order List Master Detail</h1>
        <p class="lede">
          Vue 3 proof of concept for replacing the legacy Gizmox/WebForms job list with a real ASP.NET Core API pilot.
        </p>
      </div>
      <form class="control-panel" @submit.prevent="refreshData">
        <label>
          API base URL
          <input v-model="state.apiBaseUrl" type="url" />
        </label>
        <label>
          Start date
          <input v-model="state.startOn" type="date" />
        </label>
        <label>
          Days
          <input v-model.number="state.days" type="number" min="1" max="31" />
        </label>
        <label>
          Demo role
          <select v-model="state.role">
            <option>Admin</option>
            <option>Manager</option>
            <option>Viewer</option>
          </select>
        </label>
        <button :disabled="state.isLoading" type="submit">
          {{ state.isLoading ? 'Refreshing...' : 'Refresh jobs' }}
        </button>
      </form>
    </section>

    <p v-if="state.error" class="error-banner">{{ state.error }}</p>

    <section class="workspace">
      <aside class="job-list card">
        <div class="panel-heading">
          <h2>Jobs in range</h2>
          <span>{{ state.jobs.length }} items</span>
        </div>
        <button
          v-for="job in state.jobs"
          :key="job.orderId"
          class="job-row"
          :class="{ selected: job.orderId === state.selectedJob?.orderId }"
          type="button"
          @click="selectJob(job.orderId)"
        >
          <strong>{{ job.orderNumber }}</strong>
          <span>{{ job.customerName }}</span>
          <small>{{ job.orderTitle }}</small>
          <em>Required {{ new Date(job.requiredOn).toLocaleDateString() }}</em>
        </button>
      </aside>

      <article class="detail card" v-if="state.selectedJob">
        <div class="panel-heading">
          <div>
            <p class="eyebrow">Selected job</p>
            <h2>{{ selectedSummary }}</h2>
          </div>
          <span class="status-pill">Status {{ state.selectedJob.status }}</span>
        </div>

        <dl class="detail-grid">
          <div>
            <dt>Order number</dt>
            <dd>{{ state.selectedJob.orderNumber }}</dd>
          </div>
          <div>
            <dt>Customer</dt>
            <dd>{{ state.selectedJob.customerName }}</dd>
          </div>
          <div>
            <dt>Reference</dt>
            <dd>{{ state.selectedJob.customerRef }}</dd>
          </div>
          <div>
            <dt>Ordered by</dt>
            <dd>{{ state.selectedJob.orderedBy }}</dd>
          </div>
          <div>
            <dt>Required on</dt>
            <dd>{{ new Date(state.selectedJob.requiredOn).toLocaleString() }}</dd>
          </div>
          <div>
            <dt>Quantity</dt>
            <dd>{{ state.selectedJob.qty }}</dd>
          </div>
        </dl>

        <section class="subpanel">
          <h3>Style titles</h3>
          <ul>
            <li v-for="styleTitle in state.selectedJob.styleTitles" :key="styleTitle">{{ styleTitle }}</li>
          </ul>
        </section>

        <section class="subpanel">
          <h3>Attachments</h3>
          <ul>
            <li v-for="attachment in state.selectedJob.attachments" :key="attachment.fileName">
              {{ attachment.attachmentType }} - {{ attachment.fileName }}
            </li>
          </ul>
        </section>

        <section class="subpanel note-panel">
          <h3>Operator notes</h3>
          <p>{{ state.selectedJob.remarks }}</p>
        </section>
      </article>

      <article class="detail card empty-state" v-else>
        <h2>No job selected</h2>
        <p>Load the API pilot to inspect one of the migrated master-detail records.</p>
      </article>
    </section>
  </div>
</template>
