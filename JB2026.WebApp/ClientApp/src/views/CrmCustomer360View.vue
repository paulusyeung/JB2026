<template>
  <section class="customer-360-page" :class="{ 'is-dragging': isDragging }">
    <div class="resize-overlay" v-if="isDragging" @mousemove="onMouseMove" @mouseup="stopResize" />
    <div class="left-pane">
      <v-card rounded="xl" elevation="0" class="panel-card company-select-card">
        <v-card-text>
          <v-autocomplete
            v-model="selectedCompanyId"
            :items="companies"
            item-title="name"
            item-value="id"
            :label="t('crm.companies.lookup')"
            prepend-inner-icon="mdi-domain"
            variant="solo-filled"
            density="comfortable"
            hide-details
            clearable
            :loading="loadingCompanies"
            :search="companySearch"
            @update:search="onCompanySearch"
            @update:model-value="onCompanySelected"
          />

          <v-divider class="my-3" />

          <div v-if="loadingCompany" class="d-flex justify-center py-6">
            <v-progress-circular indeterminate size="24" />
          </div>

          <template v-else-if="company">
            <div class="company-info">
              <div class="company-info-header">
                <h4 class="text-h6 mb-0">{{ company.name }}</h4>
                <v-btn icon="mdi-pencil" variant="flat" size="small" color="default" class="edit-btn" @click="openEditDialog" />
              </div>

              <div class="info-row">
                <v-icon size="small" class="mr-1">mdi-account-tie</v-icon>
                <span class="text-body-2">{{ company.accountOwner }}</span>
              </div>

              <div v-if="company.domainName" class="info-row">
                <v-icon size="small" class="mr-1">mdi-web</v-icon>
                <span class="text-body-2">{{ company.domainName }}</span>
              </div>

              <div v-if="company.formattedAddress" class="info-row">
                <v-icon size="small" class="mr-1">mdi-map-marker</v-icon>
                <span class="text-body-2">{{ company.formattedAddress }}</span>
              </div>

              <div v-if="company.people.length === 0" class="info-row">
                <v-icon size="small" class="mr-1">mdi-account-multiple</v-icon>
                <span class="text-body-2">0 people</span>
              </div>
              <div v-else class="people-section">
                <div class="people-label">
                  <v-icon size="small">mdi-account-multiple</v-icon>
                  <span class="text-body-2">{{ company.people.length }} people</span>
                </div>
                <div class="people-cards">
                  <div v-for="person in company.people" :key="person.id" class="person-card">
                    <div class="person-card-header">
                      <span class="text-body-2 font-weight-medium">{{ person.name }}</span>
                      <v-btn icon="mdi-pencil" variant="flat" size="x-small" color="default" class="edit-btn" @click="openPersonDialog(person.id)" />
                    </div>
                    <div class="person-card-body">
                      <div v-if="getPerson(person.id)?.jobTitle" class="person-detail">
                        <v-icon size="x-small">mdi-badge-account-outline</v-icon>
                        <span class="text-caption">{{ getPerson(person.id)?.jobTitle }}</span>
                      </div>
                      <div v-for="email in getPerson(person.id)?.emails ?? []" :key="email" class="person-detail">
                        <v-icon size="x-small">mdi-email-outline</v-icon>
                        <span class="text-caption">{{ email }}</span>
                      </div>
                      <div v-for="phone in getPerson(person.id)?.phones ?? []" :key="phone" class="person-detail">
                        <v-icon size="x-small">mdi-phone-outline</v-icon>
                        <span class="text-caption">{{ phone }}</span>
                      </div>
                    </div>
                  </div>
                </div>
              </div>

              <div class="info-row">
                <v-icon size="small" class="mr-1">mdi-trending-up</v-icon>
                <span class="text-body-2">{{ company.opportunities.length }} opportunities</span>
              </div>

              <div class="info-row">
                <v-icon size="small" class="mr-1">mdi-calendar</v-icon>
                <span class="text-body-2">Created {{ companyFormat(company.createdOn) }}</span>
              </div>
            </div>
          </template>

          <div v-else class="text-center py-6 text-medium-emphasis text-body-2">
            {{ t('customer360.selectCompany') }}
          </div>
        </v-card-text>
      </v-card>
    </div>

    <div class="splitter" @mousedown="startResize" />
    <div class="right-pane">
      <v-card rounded="xl" elevation="0" class="panel-card detail-tabs-card">
        <v-tabs v-model="activeTab" fixed-tabs bg-color="transparent" color="primary">
          <v-tab value="job-orders">
            <v-icon start>mdi-briefcase-outline</v-icon>
            {{ t('customer360.tabs.jobOrders') }}
          </v-tab>
          <v-tab value="invoices">
            <v-icon start>mdi-receipt-text-outline</v-icon>
            {{ t('customer360.tabs.invoices') }}
          </v-tab>
          <v-tab value="opportunities">
            <v-icon start>mdi-trending-up</v-icon>
            {{ t('customer360.tabs.opportunities') }}
          </v-tab>
          <v-tab value="tasks">
            <v-icon start>mdi-format-list-checks</v-icon>
            {{ t('customer360.tabs.tasks') }}
          </v-tab>
          <v-tab value="files">
            <v-icon start>mdi-file-outline</v-icon>
            {{ t('customer360.tabs.files') }}
          </v-tab>
          <v-tab value="emails">
            <v-icon start>mdi-email-outline</v-icon>
            {{ t('customer360.tabs.emails') }}
          </v-tab>
          <v-tab value="calendar">
            <v-icon start>mdi-calendar-outline</v-icon>
            {{ t('customer360.tabs.calendar') }}
          </v-tab>
          <v-tab value="timeline">
            <v-icon start>mdi-timeline-outline</v-icon>
            {{ t('customer360.tabs.timeline') }}
          </v-tab>
        </v-tabs>

        <v-divider />

        <v-tabs-window v-model="activeTab">
          <v-tabs-window-item value="job-orders">
            <div class="tab-content job-orders-tab-content">
              <div class="filter-bar">
                <v-text-field
                  v-model="joLookup"
                  density="comfortable"
                  :label="t('jobOrder.jobList.lookup')"
                  prepend-inner-icon="mdi-magnify"
                  variant="solo-filled"
                  hide-details
                  clearable
                  @keydown.enter="applyJoLookup"
                  @click:clear="clearJoLookup"
                />
                <v-btn color="primary" prepend-icon="mdi-magnify" :loading="loadingJobOrders" @click="applyJoLookup">
                  {{ t('jobOrder.jobList.search') }}
                </v-btn>
                <v-btn variant="tonal" prepend-icon="mdi-refresh" :loading="loadingJobOrders" @click="refreshJoList">
                  {{ t('jobOrder.jobList.actions.refresh') }}
                </v-btn>
              </div>

              <v-alert v-if="joErrorMessage" type="warning" variant="tonal" class="mt-3 mb-2">{{ joErrorMessage }}</v-alert>

              <div class="toolbar-bar mb-2">
                <v-menu location="bottom">
                  <template #activator="{ props }">
                    <v-btn v-bind="props" variant="outlined" size="small" prepend-icon="mdi-view-column">
                      {{ t('jobOrder.jobList.actions.columns') }}
                    </v-btn>
                  </template>
                  <v-list density="compact" class="toolbar-menu-list">
                    <v-list-item v-for="column in joColumnOptions" :key="column.key" @click="toggleJoColumn(column.key)">
                      <template #prepend>
                        <v-checkbox-btn :model-value="joVisibleColumnKeys.includes(column.key)" />
                      </template>
                      <v-list-item-title>{{ column.title }}</v-list-item-title>
                    </v-list-item>
                  </v-list>
                </v-menu>

                <v-menu location="bottom">
                  <template #activator="{ props }">
                    <v-btn v-bind="props" variant="outlined" size="small" prepend-icon="mdi-sort">
                      {{ t('jobOrder.jobList.actions.sorting') }}
                    </v-btn>
                  </template>
                  <v-card min-width="280" class="pa-3">
                    <v-select
                      v-model="joSortKey"
                      :items="joSortableColumns"
                      item-title="title"
                      item-value="key"
                      density="compact"
                      variant="outlined"
                      :label="t('jobOrder.jobList.actions.sortBy')"
                      hide-details
                    />
                    <v-btn-toggle v-model="joSortDirection" mandatory divided class="mt-3" density="compact">
                      <v-btn value="asc">{{ t('jobOrder.jobList.actions.asc') }}</v-btn>
                      <v-btn value="desc">{{ t('jobOrder.jobList.actions.desc') }}</v-btn>
                    </v-btn-toggle>
                  </v-card>
                </v-menu>

                <template v-if="!isPhoneLayout">
                  <v-btn variant="outlined" size="small" prepend-icon="mdi-checkbox-multiple-marked-outline" @click="joCheckboxMode = !joCheckboxMode">
                    {{ t('jobOrder.jobList.actions.checkbox') }}
                  </v-btn>

                  <v-menu location="bottom">
                    <template #activator="{ props }">
                      <v-btn v-bind="props" variant="outlined" size="small" prepend-icon="mdi-eye-outline">
                        {{ t('jobOrder.jobList.actions.views') }}
                      </v-btn>
                    </template>
                    <v-list density="compact" class="toolbar-menu-list">
                      <v-list-item prepend-icon="mdi-table" :active="joViewMode === 'detail'" @click="setJoViewMode('detail')">
                        <v-list-item-title>{{ t('jobOrder.jobList.actions.detailView') }}</v-list-item-title>
                      </v-list-item>
                      <v-list-item prepend-icon="mdi-view-grid-outline" :active="joViewMode === 'card'" @click="setJoViewMode('card')">
                        <v-list-item-title>{{ t('jobOrder.jobList.actions.cardView') }}</v-list-item-title>
                      </v-list-item>
                    </v-list>
                  </v-menu>

                  <v-divider vertical class="mx-1" />

                  <v-btn variant="outlined" size="small" color="primary" prepend-icon="mdi-file-plus" @click="openNewJobOrder">
                    {{ t('jobOrder.jobList.actions.newOrder') }}
                  </v-btn>

                  <v-btn
                    variant="tonal"
                    color="error"
                    size="small"
                    prepend-icon="mdi-delete"
                    :disabled="joSelectedIds.length === 0 || joDeleting"
                    :loading="joDeleting"
                    @click="confirmJoBatchDelete"
                  >
                    {{ t('jobOrder.jobList.actions.deleteSelected') }}
                  </v-btn>
                </template>

                <v-menu v-else location="bottom end">
                  <template #activator="{ props }">
                    <v-btn v-bind="props" variant="outlined" size="small" prepend-icon="mdi-dots-horizontal">
                      {{ t('jobOrder.jobList.actions.more') }}
                    </v-btn>
                  </template>
                  <v-list density="compact" class="toolbar-menu-list">
                    <v-list-item prepend-icon="mdi-checkbox-multiple-marked-outline" @click="joCheckboxMode = !joCheckboxMode">
                      <v-list-item-title>{{ t('jobOrder.jobList.actions.checkbox') }}</v-list-item-title>
                    </v-list-item>
                    <v-list-item prepend-icon="mdi-table" :active="joViewMode === 'detail'" @click="setJoViewMode('detail')">
                      <v-list-item-title>{{ t('jobOrder.jobList.actions.detailView') }}</v-list-item-title>
                    </v-list-item>
                    <v-list-item prepend-icon="mdi-view-grid-outline" :active="joViewMode === 'card'" @click="setJoViewMode('card')">
                      <v-list-item-title>{{ t('jobOrder.jobList.actions.cardView') }}</v-list-item-title>
                    </v-list-item>
                    <v-list-item prepend-icon="mdi-file-plus" @click="openNewJobOrder">
                      <v-list-item-title>{{ t('jobOrder.jobList.actions.newOrder') }}</v-list-item-title>
                    </v-list-item>
                    <v-list-item prepend-icon="mdi-delete" :disabled="joSelectedIds.length === 0 || joDeleting" @click="confirmJoBatchDelete">
                      <v-list-item-title>{{ t('jobOrder.jobList.actions.deleteSelected') }}</v-list-item-title>
                    </v-list-item>
                  </v-list>
                </v-menu>

                <span v-if="joCheckboxMode" class="text-caption text-medium-emphasis">
                  {{ t('jobOrder.jobList.actions.selected', { count: joSelectedIds.length }) }}
                </span>
              </div>

              <div v-if="!company" class="text-center py-6 text-medium-emphasis text-body-2">
                {{ t('customer360.selectCompany') }}
              </div>

              <template v-else-if="joRows.length === 0">
                <div class="text-center py-6 text-medium-emphasis text-body-2">
                  {{ t('customer360.placeholders.jobOrders') }}
                </div>
              </template>

              <template v-else>
                <div v-if="isJoCardView" class="jo-card-list">
                  <v-card
                    v-for="row in joDisplayedRows"
                    :key="row.orderId"
                    rounded="lg"
                    elevation="0"
                    class="jo-card"
                    @click="openJoEditor(row)"
                  >
                    <v-checkbox-btn
                      v-if="joCheckboxMode"
                      :model-value="joSelectedIds.includes(row.orderId)"
                      density="compact"
                      hide-details
                      class="jo-card__checkbox"
                      @click.stop="handleJoCardCheckbox(row.orderId)"
                    />
                    <div class="jo-card__header">
                      <div class="d-flex align-center ga-2">
                        <v-icon size="18" :color="joOrderTypeMeta(row.orderType).color">
                          {{ joOrderTypeMeta(row.orderType).icon }}
                        </v-icon>
                        <div>
                          <div class="text-subtitle-2 font-weight-bold">{{ joCompositeNumber(row) }}</div>
                          <div class="text-caption text-medium-emphasis">{{ row.customerName || '-' }}</div>
                        </div>
                      </div>
                    </div>
                    <div class="jo-card__body">
                      <div class="d-flex align-center ga-2 mb-2">
                        <v-chip size="small" :color="joStatusColor(row.status)" variant="tonal">
                          <v-tooltip :text="joStatusLabel(row.status)" location="top">
                            <template v-slot:activator="{ props }">
                              <v-icon v-bind="props" start size="12" :color="joStatusColor(row.status)">{{ joStatusIcon(row.status) }}</v-icon>
                            </template>
                          </v-tooltip>
                          {{ joStatusLabel(row.status) }}
                        </v-chip>
                        <span class="text-caption">{{ row.orderTitle || '-' }}</span>
                      </div>
                      <div class="jo-card__metrics">
                        <span class="text-caption">{{ t('jobOrder.jobList.headers.quotation') }}: {{ row.productStyle || '-' }}</span>
                        <span class="text-caption font-weight-medium">{{ t('jobOrder.jobList.headers.invoiceAmount') }}: {{ joFormatCurrency(row.invoiceAmount) }}</span>
                      </div>
                    </div>
                    <div class="jo-card__footer text-caption text-medium-emphasis">
                      <span>{{ t('jobOrder.jobList.headers.orderedOn') }}: {{ joFormat(row.orderedOn) }}</span>
                      <span>{{ t('jobOrder.jobList.headers.requiredOn') }}: {{ joFormat(row.requiredOn) }}</span>
                    </div>
                    <div class="jo-card__meta text-caption text-medium-emphasis">
                      <span>{{ t('jobOrder.jobList.headers.modifiedBy') }}: {{ row.modifiedBy || '-' }}</span>
                      <span>{{ t('jobOrder.jobList.headers.modifiedOn') }}: {{ joFormat(row.modifiedOn) }}</span>
                    </div>
                  </v-card>
                </div>
                <v-data-table
                  v-else
                  :headers="joHeaders"
                  :items="joDisplayedRows"
                  :loading="loadingJobOrders"
                  item-value="orderId"
                  v-model="joSelectedIds"
                  :show-select="joCheckboxMode"
                  density="compact"
                  fixed-header
                  height="45vh"
                  class="job-orders-table"
                  @click:row="onJoRowClick"
                >
                  <template #[`item.ln`]="{ index }">{{ index + 1 }}</template>

                  <template #[`header.orderType`]>
                    <!-- <span class="sr-only">{{ t('jobOrder.jobList.headers.orderType') }}</span> -->
                    <v-icon size="14" color="primary">mdi-tag-outline</v-icon>
                  </template>

                  <template #[`header.status`]>
                    <!-- <span class="sr-only">{{ t('jobOrder.jobList.headers.status') }}</span> -->
                    <v-icon size="14" color="primary">mdi-flag</v-icon>
                  </template>

                  <template #[`header.attachProduct`]>
                    <!-- <span class="sr-only">{{ t('jobOrder.jobList.headers.attachProduct') }}</span> -->
                    <v-icon size="14" color="primary">mdi-paperclip</v-icon>
                  </template>

                  <template #[`header.attachCustomer`]>
                    <!-- <span class="sr-only">{{ t('jobOrder.jobList.headers.attachCustomer') }}</span> -->
                    <v-icon size="14" color="primary">mdi-paperclip</v-icon>
                  </template>

                  <template #[`item.orderType`]="{ item }">
                    <div class="d-flex justify-center">
                      <v-icon size="16" :color="joOrderTypeMeta(item.orderType).color">{{ joOrderTypeMeta(item.orderType).icon }}</v-icon>
                    </div>
                  </template>

                  <template #[`item.orderNumber`]="{ item }">
                    <v-btn variant="text" color="primary" density="comfortable" class="px-0 text-none" @click.stop="openJoEditor(item)">
                      {{ joCompositeNumber(item) }}
                    </v-btn>
                  </template>

                  <template #[`item.status`]="{ item }">
                    <div class="d-flex justify-center">
                      <v-tooltip :text="joStatusLabel(item.status)" location="top">
                        <template v-slot:activator="{ props }">
                          <v-icon v-bind="props" size="16" :color="joStatusColor(item.status)">{{ joStatusIcon(item.status) }}</v-icon>
                        </template>
                      </v-tooltip>
                    </div>
                  </template>

                  <template #[`item.attachProduct`]="{ item }">
                    <div class="d-flex justify-center">
                      <v-icon size="14" :color="item.attachmentProductCount > 0 ? 'success' : 'error'">
                        {{ item.attachmentProductCount > 0 ? 'mdi-paperclip' : 'mdi-circle-outline' }}
                      </v-icon>
                    </div>
                  </template>

                  <template #[`item.attachCustomer`]="{ item }">
                    <div class="d-flex justify-center">
                      <v-icon size="14" :color="item.attachmentCustomerCount > 0 ? 'success' : 'error'">
                        {{ item.attachmentCustomerCount > 0 ? 'mdi-paperclip' : 'mdi-circle-outline' }}
                      </v-icon>
                    </div>
                  </template>

                  <template #[`item.orderedOn`]="{ item }">{{ joFormat(item.orderedOn) }}</template>
                  <template #[`item.requiredOn`]="{ item }">{{ joFormat(item.requiredOn) }}</template>
                  <template #[`item.completedOn`]="{ item }">{{ item.completedOn ? joFormat(item.completedOn) : '-' }}</template>
                  <template #[`item.modifiedOn`]="{ item }">{{ item.modifiedOn ? joFormat(item.modifiedOn) : '-' }}</template>
                  <template #[`item.modifiedBy`]="{ item }">{{ item.modifiedBy || '-' }}</template>
                  <template #[`item.invoiceAmount`]="{ item }">{{ joFormatCurrency(item.invoiceAmount) }}</template>
                </v-data-table>
              </template>
            </div>
          </v-tabs-window-item>
          <v-tabs-window-item value="invoices">
            <div class="tab-content invoices-tab-content">
              <div class="filter-bar">
                <v-text-field
                  v-model="invInvoiceLookup"
                  density="comfortable"
                  :label="t('billing.invoices.invoiceLookup')"
                  prepend-inner-icon="mdi-file-document-outline"
                  variant="solo-filled"
                  hide-details
                  clearable
                  @keydown.enter="applyInvLookup"
                />
                <v-btn color="primary" prepend-icon="mdi-magnify" :loading="loadingInvoices" @click="applyInvLookup">
                  {{ t('common.search') }}
                </v-btn>
                <v-btn variant="tonal" prepend-icon="mdi-refresh" :loading="loadingInvoices" @click="refreshInvList">
                  {{ t('common.refresh') }}
                </v-btn>
              </div>

              <v-alert v-if="invErrorMessage" type="warning" variant="tonal" class="mt-2 mb-2">{{ invErrorMessage }}</v-alert>

              <div class="toolbar-bar mb-2">
                <v-menu location="bottom">
                  <template #activator="{ props }">
                    <v-btn v-bind="props" variant="outlined" size="small" prepend-icon="mdi-view-column">
                      {{ t('billing.invoices.actions.columns') }}
                    </v-btn>
                  </template>
                  <v-list density="compact" class="toolbar-menu-list">
                    <v-list-item v-for="column in invColumnOptions" :key="column.key" @click="toggleInvColumn(column.key)">
                      <template #prepend>
                        <v-checkbox-btn :model-value="invVisibleColumnKeys.includes(column.key)" />
                      </template>
                      <v-list-item-title>{{ column.title }}</v-list-item-title>
                    </v-list-item>
                  </v-list>
                </v-menu>

                <v-menu location="bottom">
                  <template #activator="{ props }">
                    <v-btn v-bind="props" variant="outlined" size="small" prepend-icon="mdi-sort">
                      {{ t('billing.invoices.actions.sorting') }}
                    </v-btn>
                  </template>
                  <v-card min-width="280" class="pa-3">
                    <v-select
                      v-model="invSortKey"
                      :items="invSortableColumns"
                      item-title="title"
                      item-value="key"
                      density="compact"
                      variant="outlined"
                      :label="t('billing.invoices.actions.sortBy')"
                      hide-details
                    />
                    <v-btn-toggle v-model="invSortDirection" mandatory divided class="mt-3" density="compact">
                      <v-btn value="asc">{{ t('billing.invoices.actions.asc') }}</v-btn>
                      <v-btn value="desc">{{ t('billing.invoices.actions.desc') }}</v-btn>
                    </v-btn-toggle>
                  </v-card>
                </v-menu>

                <template v-if="!isPhoneLayout">
                  <v-btn variant="outlined" size="small" prepend-icon="mdi-checkbox-multiple-marked-outline" @click="invCheckboxMode = !invCheckboxMode">
                    {{ t('billing.invoices.actions.checkbox') }}
                  </v-btn>

                  <v-menu location="bottom">
                    <template #activator="{ props }">
                      <v-btn v-bind="props" variant="outlined" size="small" prepend-icon="mdi-eye-outline">
                        {{ t('billing.invoices.actions.views') }}
                      </v-btn>
                    </template>
                    <v-list density="compact" class="toolbar-menu-list">
                      <v-list-item prepend-icon="mdi-table" :active="invViewMode === 'detail'" @click="setInvViewMode('detail')">
                        <v-list-item-title>{{ t('billing.invoices.actions.detailView') }}</v-list-item-title>
                      </v-list-item>
                      <v-list-item prepend-icon="mdi-view-grid-outline" :active="invViewMode === 'card'" @click="setInvViewMode('card')">
                        <v-list-item-title>{{ t('billing.invoices.actions.cardView') }}</v-list-item-title>
                      </v-list-item>
                    </v-list>
                  </v-menu>

                  <v-divider vertical class="mx-1" />

                  <v-btn variant="outlined" size="small" color="primary" prepend-icon="mdi-plus-circle-outline" @click="openNewInvoice">
                    {{ t('billing.invoices.actions.newInvoice') }}
                  </v-btn>

                  <v-btn
                    variant="outlined"
                    size="small"
                    :disabled="!isInvMarkSentEnabled || isInvSending"
                    :loading="isInvSending"
                    prepend-icon="mdi-send-circle-outline"
                    @click="handleInvMarkSent"
                  >
                    {{ t('billing.invoices.actions.markSent') }}
                  </v-btn>

                  <v-menu location="bottom">
                    <template #activator="{ props }">
                      <v-btn
                        v-bind="props"
                        variant="outlined"
                        size="small"
                        :disabled="!isInvDownloadEnabled"
                        prepend-icon="mdi-download-circle-outline"
                      >
                        {{ t('billing.invoices.actions.download') }}
                      </v-btn>
                    </template>
                    <v-list density="compact" class="toolbar-menu-list">
                      <v-list-item @click="handleInvDownloadInvoicePdf">
                        <v-list-item-title>{{ t('billing.invoices.actions.invoicePdf') }}</v-list-item-title>
                      </v-list-item>
                      <v-list-item @click="handleInvDownloadDeliveryNote">
                        <v-list-item-title>{{ t('billing.invoices.actions.deliveryNote') }}</v-list-item-title>
                      </v-list-item>
                    </v-list>
                  </v-menu>
                </template>

                <v-menu v-else location="bottom end">
                  <template #activator="{ props }">
                    <v-btn v-bind="props" variant="outlined" size="small" prepend-icon="mdi-dots-horizontal">
                      {{ t('billing.invoices.actions.more') }}
                    </v-btn>
                  </template>
                  <v-list density="compact" class="toolbar-menu-list">
                    <v-list-item prepend-icon="mdi-checkbox-multiple-marked-outline" @click="invCheckboxMode = !invCheckboxMode">
                      <v-list-item-title>{{ t('billing.invoices.actions.checkbox') }}</v-list-item-title>
                    </v-list-item>
                    <v-list-item prepend-icon="mdi-table" :active="invViewMode === 'detail'" @click="setInvViewMode('detail')">
                      <v-list-item-title>{{ t('billing.invoices.actions.detailView') }}</v-list-item-title>
                    </v-list-item>
                    <v-list-item prepend-icon="mdi-view-grid-outline" :active="invViewMode === 'card'" @click="setInvViewMode('card')">
                      <v-list-item-title>{{ t('billing.invoices.actions.cardView') }}</v-list-item-title>
                    </v-list-item>
                    <v-list-item prepend-icon="mdi-plus-circle-outline" @click="openNewInvoice">
                      <v-list-item-title>{{ t('billing.invoices.actions.newInvoice') }}</v-list-item-title>
                    </v-list-item>
                  </v-list>
                </v-menu>

                <span v-if="invCheckboxMode" class="text-caption text-medium-emphasis">
                  {{ t('billing.invoices.labels.selected', { count: invSelectedIds.length }) }}
                </span>
              </div>

              <div v-if="!company" class="text-center py-6 text-medium-emphasis text-body-2">
                {{ t('customer360.selectCompany') }}
              </div>

              <template v-else-if="isInvCardView && invRows.length > 0">
                <div class="invoice-card-list">
                  <v-card
                    v-for="invoice in invDisplayedRows"
                    :key="invoice.externalInvoiceId"
                    rounded="lg"
                    elevation="0"
                    class="invoice-card"
                    @click="openInvoice(invoice)"
                  >
                    <div v-if="invCheckboxMode" class="invoice-card__checkbox-anchor" @click.stop>
                      <v-checkbox-btn
                        class="invoice-card__checkbox"
                        :model-value="invSelectedIds.includes(invoice.externalInvoiceId)"
                        density="compact"
                        hide-details
                        @click.stop="handleInvCardCheckbox(invoice.externalInvoiceId)"
                      />
                    </div>

                    <div class="invoice-card__header">
                      <div>
                        <div class="text-subtitle-2 font-weight-bold">{{ invDisplayValue(invoice.invoiceNumber || invoice.externalInvoiceId) }}</div>
                        <div class="text-caption text-medium-emphasis">{{ invDisplayValue(invoice.clientName) }}</div>
                      </div>
                    </div>

                    <div class="invoice-card__body">
                      <span>{{ invoice.invoiceDate ? invFormat(invoice.invoiceDate) : t('billing.invoices.labels.empty') }}</span>
                      <v-chip size="small" :color="invStatusColor(invoice.status)" variant="tonal">
                        {{ invStatusLabel(invoice.status) }}
                      </v-chip>
                    </div>

                    <div class="invoice-card__footer text-caption text-medium-emphasis">
                      <span>{{ t('billing.invoices.labels.amount') }}: {{ invFormatCurrency(invoice.amount) }}</span>
                      <span>{{ t('billing.invoices.labels.due') }}: {{ invoice.dueDate ? invFormat(invoice.dueDate) : t('billing.invoices.labels.empty') }}</span>
                      <span>{{ t('billing.invoices.labels.lastSynced') }}: {{ invoice.lastSyncedAt ? invFormat(invoice.lastSyncedAt) : t('billing.invoices.labels.empty') }}</span>
                    </div>
                  </v-card>
                </div>
              </template>

              <template v-else-if="invRows.length === 0">
                <div class="text-center py-6 text-medium-emphasis text-body-2">
                  {{ t('customer360.placeholders.invoices') }}
                </div>
              </template>

              <template v-else>
                <v-data-table
                  v-model="invSelectedIds"
                  :headers="invHeaders"
                  :items="invDisplayedRows"
                  :loading="loadingInvoices"
                  item-value="externalInvoiceId"
                  :show-select="invCheckboxMode"
                  density="compact"
                  fixed-header
                  height="45vh"
                  class="invoices-table"
                  @click:row="onInvRowClick"
                >
                  <template #[`item.invoiceNumber`]="{ item }">
                    <v-btn variant="text" color="primary" class="px-0 text-none" @click.stop="openInvoice(item)">
                      {{ item.invoiceNumber || item.externalInvoiceId }}
                    </v-btn>
                  </template>

                  <template #[`item.clientName`]="{ item }">
                    {{ invDisplayValue(item.clientName) }}
                  </template>

                  <template #[`item.invoiceDate`]="{ item }">
                    {{ item.invoiceDate ? invFormat(item.invoiceDate) : t('billing.invoices.labels.empty') }}
                  </template>

                  <template #[`item.status`]="{ item }">
                    <v-chip size="small" :color="invStatusColor(item.status)" variant="tonal">
                      {{ invStatusLabel(item.status) }}
                    </v-chip>
                  </template>

                  <template #[`item.amount`]="{ item }">
                    {{ invFormatCurrency(item.amount) }}
                  </template>

                  <template #[`item.dueDate`]="{ item }">
                    {{ item.dueDate ? invFormat(item.dueDate) : t('billing.invoices.labels.empty') }}
                  </template>
                </v-data-table>
              </template>
            </div>
          </v-tabs-window-item>
          <v-tabs-window-item value="opportunities">
            <div class="tab-content opportunities-tab-content">
              <div class="filter-bar">
                <v-text-field
                  v-model="oppLookup"
                  density="comfortable"
                  :label="t('crm.opportunities.lookup')"
                  prepend-inner-icon="mdi-magnify"
                  variant="solo-filled"
                  hide-details
                  clearable
                  @keydown.enter="applyOppLookup"
                />
                <v-btn color="primary" prepend-icon="mdi-magnify" :loading="loadingOpportunities" @click="applyOppLookup">
                  {{ t('common.search') }}
                </v-btn>
                <v-btn variant="tonal" prepend-icon="mdi-refresh" :loading="loadingOpportunities" @click="refreshOppList">
                  {{ t('common.refresh') }}
                </v-btn>
              </div>

              <v-alert v-if="oppErrorMessage" type="warning" variant="tonal" class="mt-3 mb-2">{{ oppErrorMessage }}</v-alert>

              <div class="toolbar-bar mb-2">
                <v-menu location="bottom">
                  <template #activator="{ props }">
                    <v-btn v-bind="props" variant="outlined" size="small" prepend-icon="mdi-view-column">
                      {{ t('crm.opportunities.actions.columns') }}
                    </v-btn>
                  </template>
                  <v-list density="compact" class="toolbar-menu-list">
                    <v-list-item v-for="column in oppColumnOptions" :key="column.key" @click="toggleOppColumn(column.key)">
                      <template #prepend>
                        <v-checkbox-btn :model-value="oppVisibleColumnKeys.includes(column.key)" />
                      </template>
                      <v-list-item-title>{{ column.title }}</v-list-item-title>
                    </v-list-item>
                  </v-list>
                </v-menu>

                <v-menu location="bottom">
                  <template #activator="{ props }">
                    <v-btn v-bind="props" variant="outlined" size="small" prepend-icon="mdi-sort">
                      {{ t('crm.opportunities.actions.sorting') }}
                    </v-btn>
                  </template>
                  <v-card min-width="280" class="pa-3">
                    <v-select
                      v-model="oppSortKey"
                      :items="oppSortableColumns"
                      item-title="title"
                      item-value="key"
                      density="compact"
                      variant="outlined"
                      :label="t('crm.opportunities.actions.sortBy')"
                      hide-details
                    />
                    <v-btn-toggle v-model="oppSortDirection" mandatory divided class="mt-3" density="compact">
                      <v-btn value="asc">{{ t('crm.opportunities.actions.asc') }}</v-btn>
                      <v-btn value="desc">{{ t('crm.opportunities.actions.desc') }}</v-btn>
                    </v-btn-toggle>
                  </v-card>
                </v-menu>

                <template v-if="!isPhoneLayout">
                  <v-btn variant="outlined" size="small" prepend-icon="mdi-checkbox-multiple-marked-outline" @click="oppCheckboxMode = !oppCheckboxMode">
                    {{ t('crm.opportunities.actions.checkbox') }}
                  </v-btn>

                  <v-menu location="bottom">
                    <template #activator="{ props }">
                      <v-btn v-bind="props" variant="outlined" size="small" prepend-icon="mdi-eye-outline">
                        {{ t('crm.opportunities.actions.views') }}
                      </v-btn>
                    </template>
                    <v-list density="compact" class="toolbar-menu-list">
                      <v-list-item prepend-icon="mdi-table" :active="oppViewMode === 'detail'" @click="setOppViewMode('detail')">
                        <v-list-item-title>{{ t('crm.opportunities.actions.detailView') }}</v-list-item-title>
                      </v-list-item>
                      <v-list-item prepend-icon="mdi-view-grid-outline" :active="oppViewMode === 'card'" @click="setOppViewMode('card')">
                        <v-list-item-title>{{ t('crm.opportunities.actions.cardView') }}</v-list-item-title>
                      </v-list-item>
                    </v-list>
                  </v-menu>
                </template>

                <v-menu v-else location="bottom end">
                  <template #activator="{ props }">
                    <v-btn v-bind="props" variant="outlined" size="small" prepend-icon="mdi-dots-horizontal">
                      {{ t('crm.opportunities.actions.views') }}
                    </v-btn>
                  </template>
                  <v-list density="compact" class="toolbar-menu-list">
                    <v-list-item prepend-icon="mdi-checkbox-multiple-marked-outline" @click="oppCheckboxMode = !oppCheckboxMode">
                      <v-list-item-title>{{ t('crm.opportunities.actions.checkbox') }}</v-list-item-title>
                    </v-list-item>
                    <v-list-item prepend-icon="mdi-table" :active="oppViewMode === 'detail'" @click="setOppViewMode('detail')">
                      <v-list-item-title>{{ t('crm.opportunities.actions.detailView') }}</v-list-item-title>
                    </v-list-item>
                    <v-list-item prepend-icon="mdi-view-grid-outline" :active="oppViewMode === 'card'" @click="setOppViewMode('card')">
                      <v-list-item-title>{{ t('crm.opportunities.actions.cardView') }}</v-list-item-title>
                    </v-list-item>
                  </v-list>
                </v-menu>

                <v-divider vertical class="mx-1" />

                <v-btn variant="outlined" size="small" color="primary" prepend-icon="mdi-plus-circle-outline" @click="openNewOpportunity">
                  {{ t('crm.opportunities.actions.newOpportunity') }}
                </v-btn>

                <span v-if="oppCheckboxMode" class="text-caption text-medium-emphasis">
                  {{ t('crm.opportunities.actions.selected', { count: oppSelectedIds.length }) }}
                </span>
              </div>

              <div v-if="!company" class="text-center py-6 text-medium-emphasis text-body-2">
                {{ t('customer360.selectCompany') }}
              </div>

              <template v-else-if="company.opportunities.length === 0">
                <div class="text-center py-6 text-medium-emphasis text-body-2">
                  {{ t('crm.opportunities.messages.noOpportunities') }}
                </div>
              </template>

              <template v-else>
                <ListMobileCard
                  v-if="isPhoneLayout"
                  :items="oppDisplayedRows"
                  :columns="oppMobileColumns"
                  item-key="id"
                  :checkbox-mode="oppCheckboxMode"
                  :selected-ids="oppSelectedIds"
                  :on-select="handleOppMobileSelect"
                  :on-card-click="(item) => onOppMobileCardClick(item)"
                />

                <div v-else-if="isOppCardView" class="opportunity-card-list">
                  <v-card
                    v-for="row in oppDisplayedRows"
                    :key="row.id"
                    rounded="lg"
                    elevation="0"
                    class="opportunity-card"
                  >
                    <v-checkbox-btn
                      v-if="oppCheckboxMode"
                      :model-value="oppSelectedIds.includes(row.id)"
                      density="compact"
                      hide-details
                      class="opportunity-card__checkbox"
                      @click="handleOppCardCheckbox(row.id)"
                    />
                    <div class="opportunity-card__header">
                      <div class="d-flex align-center ga-2">
                        <v-icon size="18" color="primary">mdi-trending-up</v-icon>
                        <div>
                          <span class="text-subtitle-2 font-weight-bold">{{ row.name }}</span>
                          <v-chip v-if="row.stage" size="x-small" label color="primary" variant="tonal" class="ml-1">
                            {{ oppStageLabel(row.stage) }}
                          </v-chip>
                          <div v-if="row.company" class="text-caption text-medium-emphasis">{{ row.company }}</div>
                        </div>
                      </div>
                    </div>
                    <div class="opportunity-card__body">
                      <span class="text-caption">
                        {{ t('crm.opportunities.headers.amount') }}: {{ row.amount || '-' }}
                      </span>
                      <span class="text-caption">
                        {{ t('crm.opportunities.headers.owner') }}: {{ row.owner || '-' }}
                      </span>
                    </div>
                    <div class="opportunity-card__footer text-caption text-medium-emphasis">
                      <span>{{ t('crm.opportunities.headers.updatedBy') }}: {{ row.updatedBy || '-' }}</span>
                      <span>{{ t('crm.opportunities.headers.updatedOn') }}: {{ oppFormat(row.updatedOn) }}</span>
                    </div>
                  </v-card>
                </div>

                <v-data-table
                  v-else
                  :headers="oppHeaders"
                  :items="oppDisplayedRows"
                  :loading="loadingOpportunities"
                  item-value="id"
                  v-model="oppSelectedIds"
                  :show-select="oppCheckboxMode"
                  density="compact"
                  fixed-header
                  height="45vh"
                  class="opportunities-table"
                >
                  <template #[`item.name`]='{ item }'>
                    <a class="text-body-2 text-primary text-decoration-none cursor-pointer" @click.stop="openOpportunityPopup(item.id)">{{ item.name }}</a>
                  </template>

                  <template #[`item.stage`]='{ item }'>
                    <v-chip v-if="item.stage" size="x-small" label color="primary" variant="tonal">{{ oppStageLabel(item.stage) }}</v-chip>
                    <span v-else class="text-medium-emphasis">-</span>
                  </template>

                  <template #[`item.closeDate`]='{ item }'>
                    <template v-if="item.closeDate">{{ oppFormat(item.closeDate) }}</template>
                    <span v-else class="text-medium-emphasis">-</span>
                  </template>

                  <template #[`item.amount`]='{ item }'>
                    <span class="text-right" style="display:block">{{ item.amount || '-' }}</span>
                  </template>

                  <template #[`item.company`]='{ item }'>
                    <v-chip v-if="item.company" size="small" label color="primary" variant="tonal">{{ item.company }}</v-chip>
                    <span v-else class="text-medium-emphasis">-</span>
                  </template>

                  <template #[`item.pointOfContact`]='{ item }'>
                    <v-chip v-if="item.pointOfContact" size="small" label>{{ item.pointOfContact }}</v-chip>
                    <span v-else class="text-medium-emphasis">-</span>
                  </template>

                  <template #[`item.owner`]='{ item }'>
                    {{ item.owner || '-' }}
                  </template>

                  <template #[`item.createdOn`]='{ item }'>{{ oppFormat(item.createdOn) }}</template>
                  <template #[`item.updatedOn`]='{ item }'>{{ oppFormat(item.updatedOn) }}</template>
                </v-data-table>
              </template>
            </div>
          </v-tabs-window-item>
          <v-tabs-window-item value="tasks">
            <div class="tab-content tasks-tab-content">
              <div class="filter-bar">
                <v-text-field
                  v-model="taskLookup"
                  density="comfortable"
                  :label="t('crm.tasks.lookup')"
                  prepend-inner-icon="mdi-magnify"
                  variant="solo-filled"
                  hide-details
                  clearable
                  @keydown.enter="applyTaskLookup"
                />
                <v-btn color="primary" prepend-icon="mdi-magnify" :loading="loadingTasks" @click="applyTaskLookup">
                  {{ t('common.search') }}
                </v-btn>
                <v-btn variant="tonal" prepend-icon="mdi-refresh" :loading="loadingTasks" @click="refreshTaskList">
                  {{ t('common.refresh') }}
                </v-btn>
              </div>

              <v-alert v-if="taskErrorMessage" type="warning" variant="tonal" class="mt-3 mb-2">{{ taskErrorMessage }}</v-alert>

              <div class="toolbar-bar mb-2">
                <v-menu location="bottom">
                  <template #activator="{ props }">
                    <v-btn v-bind="props" variant="outlined" size="small" prepend-icon="mdi-view-column">
                      {{ t('crm.tasks.actions.columns') }}
                    </v-btn>
                  </template>
                  <v-list density="compact" class="toolbar-menu-list">
                    <v-list-item v-for="column in taskColumnOptions" :key="column.key" @click="toggleTaskColumn(column.key)">
                      <template #prepend>
                        <v-checkbox-btn :model-value="taskVisibleColumnKeys.includes(column.key)" />
                      </template>
                      <v-list-item-title>{{ column.title }}</v-list-item-title>
                    </v-list-item>
                  </v-list>
                </v-menu>

                <v-menu location="bottom">
                  <template #activator="{ props }">
                    <v-btn v-bind="props" variant="outlined" size="small" prepend-icon="mdi-sort">
                      {{ t('crm.tasks.actions.sorting') }}
                    </v-btn>
                  </template>
                  <v-card min-width="280" class="pa-3">
                    <v-select
                      v-model="taskSortKey"
                      :items="taskSortableColumns"
                      item-title="title"
                      item-value="key"
                      density="compact"
                      variant="outlined"
                      :label="t('crm.tasks.actions.sortBy')"
                      hide-details
                    />
                    <v-btn-toggle v-model="taskSortDirection" mandatory divided class="mt-3" density="compact">
                      <v-btn value="asc">{{ t('crm.tasks.actions.asc') }}</v-btn>
                      <v-btn value="desc">{{ t('crm.tasks.actions.desc') }}</v-btn>
                    </v-btn-toggle>
                  </v-card>
                </v-menu>

                <template v-if="!isPhoneLayout">
                  <v-btn variant="outlined" size="small" prepend-icon="mdi-checkbox-multiple-marked-outline" @click="taskCheckboxMode = !taskCheckboxMode">
                    {{ t('crm.tasks.actions.checkbox') }}
                  </v-btn>

                  <v-menu location="bottom">
                    <template #activator="{ props }">
                      <v-btn v-bind="props" variant="outlined" size="small" prepend-icon="mdi-eye-outline">
                        {{ t('crm.tasks.actions.views') }}
                      </v-btn>
                    </template>
                    <v-list density="compact" class="toolbar-menu-list">
                      <v-list-item prepend-icon="mdi-table" :active="taskViewMode === 'detail'" @click="setTaskViewMode('detail')">
                        <v-list-item-title>{{ t('crm.tasks.actions.detailView') }}</v-list-item-title>
                      </v-list-item>
                      <v-list-item prepend-icon="mdi-view-grid-outline" :active="taskViewMode === 'card'" @click="setTaskViewMode('card')">
                        <v-list-item-title>{{ t('crm.tasks.actions.cardView') }}</v-list-item-title>
                      </v-list-item>
                    </v-list>
                  </v-menu>
                </template>

                <v-menu v-else location="bottom end">
                  <template #activator="{ props }">
                    <v-btn v-bind="props" variant="outlined" size="small" prepend-icon="mdi-dots-horizontal">
                      {{ t('crm.tasks.actions.views') }}
                    </v-btn>
                  </template>
                  <v-list density="compact" class="toolbar-menu-list">
                    <v-list-item prepend-icon="mdi-checkbox-multiple-marked-outline" @click="taskCheckboxMode = !taskCheckboxMode">
                      <v-list-item-title>{{ t('crm.tasks.actions.checkbox') }}</v-list-item-title>
                    </v-list-item>
                    <v-list-item prepend-icon="mdi-table" :active="taskViewMode === 'detail'" @click="setTaskViewMode('detail')">
                      <v-list-item-title>{{ t('crm.tasks.actions.detailView') }}</v-list-item-title>
                    </v-list-item>
                    <v-list-item prepend-icon="mdi-view-grid-outline" :active="taskViewMode === 'card'" @click="setTaskViewMode('card')">
                      <v-list-item-title>{{ t('crm.tasks.actions.cardView') }}</v-list-item-title>
                    </v-list-item>
                  </v-list>
                </v-menu>

                <v-divider vertical class="mx-1" />

                <v-btn variant="outlined" size="small" color="primary" prepend-icon="mdi-plus-circle-outline" @click="openNewTask">
                  {{ t('crm.tasks.actions.newTask') }}
                </v-btn>

                <span v-if="taskCheckboxMode" class="text-caption text-medium-emphasis">
                  {{ t('crm.tasks.actions.selected', { count: taskSelectedIds.length }) }}
                </span>
              </div>

              <div v-if="!company" class="text-center py-6 text-medium-emphasis text-body-2">
                {{ t('customer360.selectCompany') }}
              </div>

              <template v-else-if="taskRows.length === 0">
                <div class="text-center py-6 text-medium-emphasis text-body-2">
                  {{ t('crm.tasks.messages.noTasks') }}
                </div>
              </template>

              <template v-else>
                <ListMobileCard
                  v-if="isPhoneLayout"
                  :items="taskDisplayedRows"
                  :columns="taskMobileColumns"
                  item-key="id"
                  :checkbox-mode="taskCheckboxMode"
                  :selected-ids="taskSelectedIds"
                  :on-select="handleTaskMobileSelect"
                  :on-card-click="(item) => onTaskMobileCardClick(item)"
                />

                <div v-else-if="isTaskCardView" class="task-card-list">
                  <v-card
                    v-for="row in taskDisplayedRows"
                    :key="row.id"
                    rounded="lg"
                    elevation="0"
                    class="task-card"
                  >
                    <v-checkbox-btn
                      v-if="taskCheckboxMode"
                      :model-value="taskSelectedIds.includes(row.id)"
                      density="compact"
                      hide-details
                      class="task-card__checkbox"
                      @click="handleTaskCardCheckbox(row.id)"
                    />
                    <div class="task-card__header">
                      <div class="d-flex align-center ga-2">
                        <v-icon size="18" color="primary">mdi-format-list-checks</v-icon>
                        <div>
                          <span class="text-subtitle-2 font-weight-bold">{{ row.title }}</span>
                          <v-chip v-if="row.status" size="x-small" label :color="taskStatusColor(row.status)" variant="tonal" class="ml-1">
                            {{ taskStatusLabel(row.status) }}
                          </v-chip>
                          <div v-if="row.dueDate" class="text-caption text-medium-emphasis">
                            {{ t('crm.tasks.headers.dueDate') }}: {{ taskFormat(row.dueDate) }}
                          </div>
                        </div>
                      </div>
                    </div>
                    <div class="task-card__body">
                      <span class="text-caption">
                        {{ t('crm.tasks.headers.assignee') }}: 
                        <v-chip v-if="row.assignee" size="small" label color="primary" variant="tonal">{{ row.assignee }}</v-chip>
                        <span v-else>-</span>
                      </span>
                      <span class="text-caption" v-if="row.relations?.length">
                        <v-chip
                          v-for="rel in row.relations"
                          :key="rel.id"
                          size="small"
                          label
                          color="secondary"
                          variant="tonal"
                          class="mr-1"
                        >
                          {{ rel.name }}
                        </v-chip>
                      </span>
                    </div>
                    <div class="task-card__footer text-caption text-medium-emphasis">
                      <span>{{ t('crm.tasks.headers.updatedBy') }}: {{ row.updatedBy || '-' }}</span>
                      <span>{{ t('crm.tasks.headers.updatedOn') }}: {{ taskFormat(row.updatedOn) }}</span>
                    </div>
                  </v-card>
                </div>

                <v-data-table
                  v-else
                  :headers="taskHeaders"
                  :items="taskDisplayedRows"
                  :loading="loadingTasks"
                  item-value="id"
                  v-model="taskSelectedIds"
                  :show-select="taskCheckboxMode"
                  density="compact"
                  fixed-header
                  height="45vh"
                  class="tasks-table"
                >
                  <template #[`item.title`]='{ item }'>
                    <a class="text-body-2 text-primary text-decoration-none cursor-pointer" @click.stop="openTaskPopup(item.id)">{{ item.title }}</a>
                  </template>

                  <template #[`item.status`]='{ item }'>
                    <v-chip v-if="item.status" size="x-small" label :color="taskStatusColor(item.status)" variant="tonal">{{ taskStatusLabel(item.status) }}</v-chip>
                    <span v-else class="text-medium-emphasis">-</span>
                  </template>

                  <template #[`item.body`]='{ item }'>
                    <span class="text-medium-emphasis text-truncate d-inline-block" style="max-width: 200px">
                      {{ item.body ? taskStripHtml(item.body) : '-' }}
                    </span>
                  </template>

                  <template #[`item.dueDate`]='{ item }'>
                    <template v-if="item.dueDate">{{ taskFormat(item.dueDate) }}</template>
                    <span v-else class="text-medium-emphasis">-</span>
                  </template>

                  <template #[`item.assignee`]='{ item }'>
                    <v-chip v-if="item.assignee" size="small" label color="primary" variant="tonal">{{ item.assignee }}</v-chip>
                    <span v-else class="text-medium-emphasis">-</span>
                  </template>

                  <template #[`item.relations`]='{ item }'>
                    <template v-if="item.relations?.length">
                      <v-chip
                        v-for="rel in item.relations"
                        :key="rel.id"
                        size="small"
                        label
                        color="secondary"
                        variant="tonal"
                        class="mr-1"
                      >
                        {{ rel.name }}
                      </v-chip>
                    </template>
                    <span v-else class="text-medium-emphasis">-</span>
                  </template>

                  <template #[`item.createdOn`]='{ item }'>{{ taskFormat(item.createdOn) }}</template>
                  <template #[`item.createdBy`]='{ item }'>{{ item.createdBy || '-' }}</template>
                  <template #[`item.updatedOn`]='{ item }'>{{ taskFormat(item.updatedOn) }}</template>
                  <template #[`item.updatedBy`]='{ item }'>{{ item.updatedBy || '-' }}</template>
                </v-data-table>
              </template>
            </div>
          </v-tabs-window-item>
          <v-tabs-window-item value="files">
            <div class="tab-content files-tab-content">
              <div class="filter-bar">
                <v-text-field
                  v-model="filesLookup"
                  density="comfortable"
                  :label="t('customer360.files.lookup')"
                  prepend-inner-icon="mdi-magnify"
                  variant="solo-filled"
                  hide-details
                  clearable
                  @keydown.enter="applyFilesLookup"
                  @click:clear="clearFilesLookup"
                />
                <v-btn color="primary" prepend-icon="mdi-magnify" :loading="loadingFiles" @click="applyFilesLookup">
                  {{ t('common.search') }}
                </v-btn>
                <v-btn variant="tonal" prepend-icon="mdi-refresh" :loading="loadingFiles" @click="refreshFilesList">
                  {{ t('common.refresh') }}
                </v-btn>
              </div>

              <v-alert v-if="filesErrorMessage" type="warning" variant="tonal" class="mb-3">{{ filesErrorMessage }}</v-alert>

              <div class="toolbar-bar mb-2">
                <v-menu location="bottom">
                  <template #activator="{ props }">
                    <v-btn v-bind="props" variant="outlined" size="small" prepend-icon="mdi-view-column">
                      {{ t('customer360.files.actions.columns') }}
                    </v-btn>
                  </template>
                  <v-list density="compact" class="toolbar-menu-list">
                    <v-list-item v-for="column in filesColumnOptions" :key="column.key" @click="toggleFilesColumn(column.key)">
                      <template #prepend>
                        <v-checkbox-btn :model-value="filesVisibleColumnKeys.includes(column.key)" />
                      </template>
                      <v-list-item-title>{{ column.title }}</v-list-item-title>
                    </v-list-item>
                  </v-list>
                </v-menu>

                <v-menu location="bottom">
                  <template #activator="{ props }">
                    <v-btn v-bind="props" variant="outlined" size="small" prepend-icon="mdi-sort">
                      {{ t('customer360.files.actions.sortBy') }}
                    </v-btn>
                  </template>
                  <v-card min-width="280" class="pa-3">
                    <v-select
                      v-model="filesSortKey"
                      :items="filesSortableColumns"
                      item-title="title"
                      item-value="key"
                      density="compact"
                      variant="outlined"
                      :label="t('customer360.files.actions.sortBy')"
                      hide-details
                    />
                    <v-btn-toggle v-model="filesSortDirection" mandatory divided class="mt-3" density="compact">
                      <v-btn value="asc">{{ t('customer360.files.actions.asc') }}</v-btn>
                      <v-btn value="desc">{{ t('customer360.files.actions.desc') }}</v-btn>
                    </v-btn-toggle>
                  </v-card>
                </v-menu>

                <v-btn variant="outlined" size="small" prepend-icon="mdi-checkbox-multiple-marked-outline" @click="filesCheckboxMode = !filesCheckboxMode">
                  {{ t('customer360.files.actions.checkbox') }}
                </v-btn>

                <v-menu location="bottom">
                  <template #activator="{ props }">
                    <v-btn v-bind="props" variant="outlined" size="small" prepend-icon="mdi-eye-outline">
                      {{ t('customer360.files.actions.views') }}
                    </v-btn>
                  </template>
                  <v-list density="compact" class="toolbar-menu-list">
                    <v-list-item prepend-icon="mdi-table" :active="filesViewMode === 'detail'" @click="setFilesViewMode('detail')">
                      <v-list-item-title>{{ t('customer360.files.actions.detailView') }}</v-list-item-title>
                    </v-list-item>
                    <v-list-item prepend-icon="mdi-view-grid-outline" :active="filesViewMode === 'card'" @click="setFilesViewMode('card')">
                      <v-list-item-title>{{ t('customer360.files.actions.cardView') }}</v-list-item-title>
                    </v-list-item>
                  </v-list>
                </v-menu>

                <span v-if="filesCheckboxMode" class="text-caption text-medium-emphasis">
                  {{ t('customer360.files.actions.selected', { count: filesSelectedIds.length }) }}
                </span>
              </div>

              <div v-if="!company" class="text-center py-6 text-medium-emphasis text-body-2">
                {{ t('customer360.selectCompany') }}
              </div>

              <div v-else-if="loadingFiles" class="d-flex justify-center py-6">
                <v-progress-circular indeterminate size="24" />
              </div>

              <template v-else-if="filesDisplayedRows.length === 0">
                <div class="text-center py-6 text-medium-emphasis text-body-2">
                  {{ t('customer360.placeholders.files') }}
                </div>
              </template>

              <template v-else>
                <div v-if="isFilesCardView" class="file-card-list">
                  <v-card
                    v-for="doc in filesDisplayedRows"
                    :key="doc.id"
                    rounded="lg"
                    elevation="0"
                    class="file-card"
                  >
                    <div
                      class="file-card__thumbnail"
                      :style="{ backgroundImage: doc.thumbnail ? `url(${doc.thumbnail})` : undefined }"
                    >
                      <v-tooltip :text="t('customer360.files.preview')" location="top">
                        <template #activator="{ props }">
                          <v-btn
                            v-bind="props"
                            icon="mdi-eye"
                            variant="text"
                            size="small"
                            color="red"
                            class="file-card__thumbnail-open-btn"
                            @click.stop="openFile(doc)"
                          />
                        </template>
                      </v-tooltip>
                      <div class="file-card__thumbnail-fallback">
                        <v-icon size="36" :color="fileIconColor(doc.mimeType)">{{ fileIcon(doc.mimeType) }}</v-icon>
                      </div>
                    </div>
                    <div class="file-card__header">
                      <span class="text-subtitle-2 font-weight-medium text-truncate d-block">{{ doc.title }}</span>
                      <span v-if="doc.correspondentName" class="text-caption text-medium-emphasis">{{ doc.correspondentName }}</span>
                    </div>
                    <div v-if="doc.tags?.length" class="file-card__tags">
                      <v-chip
                        v-for="tag in doc.tags"
                        :key="tag.id"
                        size="x-small"
                        label
                        variant="flat"
                        class="px-1"
                        :style="{ backgroundColor: tag.color + '30', color: tag.color, border: '1px solid ' + tag.color + '60' }"
                      >
                        {{ tag.name }}
                      </v-chip>
                    </div>
                    <div class="file-card__body">
                      <div class="file-card__body-row">
                        <span class="text-caption text-medium-emphasis">{{ t('customer360.files.documentType') }}:</span>
                        <span class="text-caption">{{ doc.documentTypeName || '—' }}</span>
                      </div>
                      <div class="file-card__body-row">
                        <span class="text-caption text-medium-emphasis">{{ t('customer360.files.created') }}:</span>
                        <span class="text-caption">{{ formatFileDate(doc.created) }}</span>
                      </div>
                      <div class="file-card__body-row">
                        <span class="text-caption text-medium-emphasis">{{ t('customer360.files.pages') }}:</span>
                        <span class="text-caption">{{ doc.pageCount ?? '—' }}</span>
                      </div>
                      <div class="file-card__body-row">
                        <span class="text-caption text-medium-emphasis">{{ t('customer360.files.owner') }}:</span>
                        <span class="text-caption">{{ doc.ownerName || '—' }}</span>
                      </div>
                      <div v-if="doc.isSharedByRequester" class="file-card__body-row">
                        <v-chip size="x-small" label color="success" variant="tonal">{{ t('common.yes') }}</v-chip>
                      </div>
                    </div>
                    <div class="file-card__footer" style="display: none">
                      <v-btn icon="mdi-open-in-new" variant="text" size="small" color="primary" @click="openFile(doc)" />
                    </div>
                  </v-card>
                </div>
                <v-data-table
                  v-else
                  :headers="filesHeaders"
                  :items="filesDisplayedRows"
                  :loading="loadingFiles"
                  item-value="id"
                  v-model="filesSelectedIds"
                  :show-select="filesCheckboxMode"
                  density="compact"
                  fixed-header
                  height="45vh"
                  class="files-table"
                >
                  <template #[`item.archiveSerialNumber`]="{ value }">
                    {{ value ?? '—' }}
                  </template>
                  <template #[`item.correspondentName`]="{ item }">
                    <v-chip v-if="item.correspondentName" size="small" label variant="tonal" color="primary">{{ item.correspondentName }}</v-chip>
                    <span v-else class="text-medium-emphasis">—</span>
                  </template>
                  <template #[`item.title`]="{ item }">
                    <div class="d-flex flex-column" style="min-width: 0;">
                      <span class="text-truncate d-inline-block" style="max-width: 280px;">
                        <v-icon size="14" class="mr-1">{{ fileIcon(item.mimeType) }}</v-icon>
                        {{ item.title }}
                      </span>
                      <div v-if="item.tags?.length" class="d-flex flex-wrap ga-1 mt-1">
                        <v-chip
                          v-for="tag in item.tags"
                          :key="tag.id"
                          size="small"
                          label
                          variant="flat"
                          class="px-1"
                          :style="{ backgroundColor: tag.color + '30', color: tag.color, border: '1px solid ' + tag.color + '60' }"
                        >
                          {{ tag.name }}
                        </v-chip>
                      </div>
                    </div>
                  </template>
                  <template #[`item.ownerName`]="{ value }">
                    {{ value || '—' }}
                  </template>
                  <template #[`item.noteCount`]="{ value }">
                    <span v-if="value && value > 0" class="d-flex align-center justify-center ga-1">
                      <v-icon size="14" color="medium-emphasis">mdi-sticker-text-outline</v-icon>
                      {{ value }}
                    </span>
                    <span v-else class="text-medium-emphasis">—</span>
                  </template>
                  <template #[`item.documentTypeName`]="{ item }">
                    <v-chip v-if="item.documentTypeName" size="small" label variant="tonal">{{ item.documentTypeName }}</v-chip>
                    <span v-else class="text-medium-emphasis">—</span>
                  </template>
                  <template #[`item.created`]="{ value }">
                    {{ formatFileDate(value) }}
                  </template>
                  <template #[`item.pageCount`]="{ value }">
                    {{ value }}
                  </template>
                  <template #[`item.isSharedByRequester`]="{ value }">
                    <v-chip v-if="value" size="small" label color="success" variant="tonal">{{ t('common.yes') }}</v-chip>
                    <v-chip v-else size="small" label variant="tonal">{{ t('common.no') }}</v-chip>
                  </template>
                  <template #[`item.actions`]="{ item }">
                    <v-btn icon="mdi-open-in-new" variant="text" size="small" color="primary" @click="openFile(item)" />
                  </template>
                </v-data-table>
              </template>
            </div>
          </v-tabs-window-item>
          <v-tabs-window-item value="emails">
            <div class="tab-content">
              <p class="text-body-2 text-medium-emphasis">{{ t('customer360.placeholders.emails') }}</p>
            </div>
          </v-tabs-window-item>
          <v-tabs-window-item value="calendar">
            <div class="tab-content">
              <p class="text-body-2 text-medium-emphasis">{{ t('customer360.placeholders.calendar') }}</p>
            </div>
          </v-tabs-window-item>
          <v-tabs-window-item value="timeline">
            <div class="tab-content timeline-tab-content">
              <div v-if="!company" class="text-center py-6 text-medium-emphasis text-body-2">
                {{ t('customer360.selectCompany') }}
              </div>

              <template v-else-if="loadingTimeline">
                <div class="d-flex justify-center py-6">
                  <v-progress-circular indeterminate size="24" />
                </div>
              </template>

              <template v-else-if="timelineItems.length === 0">
                <div class="text-center py-6 text-medium-emphasis text-body-2">
                  {{ t('customer360.placeholders.timeline') }}
                </div>
              </template>

              <v-timeline v-else side="end">
                <v-timeline-item
                  v-for="item in timelineItems"
                  :key="item.id"
                  :dot-color="timelineDotColor(item.type)"
                  size="small"
                >
                  <template v-slot:icon>
                    <v-icon>{{ timelineIcon(item.type) }}</v-icon>
                  </template>
                  <template v-slot:opposite>
                    <div class="timeline-opposite">
                      <div class="text-body-2 font-weight-medium">{{ item.title || t('customer360.timeline.untitled') }}</div>
                      <div class="text-caption text-medium-emphasis">
                        {{ timelineFormat(item.createdOn) }}
                        <span v-if="item.createdBy">{{ t('customer360.timeline.by', { name: item.createdBy }) }}</span>
                      </div>
                    </div>
                  </template>
                  <v-card v-if="item.body" rounded="lg" elevation="0" class="timeline-card" variant="outlined">
                    <v-card-text class="text-body-2">
                      {{ item.body }}
                    </v-card-text>
                  </v-card>
                </v-timeline-item>
              </v-timeline>
            </div>
          </v-tabs-window-item>
        </v-tabs-window>
      </v-card>
    </div>
    <v-dialog v-model="dialogOpen" max-width="min(100%, 760px)" scrollable>
      <CrmCompanyRecordDialog
        :company-id="editingCompanyId"
        @saved="handleSaved"
        @cancel="dialogOpen = false"
      />
    </v-dialog>

    <v-dialog v-model="personDialogOpen" max-width="min(100%, 760px)" scrollable>
      <CrmPeopleRecordDialog
        :person-id="editingPersonId"
        @saved="handlePersonSaved"
        @cancel="personDialogOpen = false"
      />
    </v-dialog>

    <v-dialog v-model="oppDialogOpen" max-width="min(100%, 760px)" scrollable>
      <CrmOpportunityRecordDialog
        :opportunity-id="editingOpportunityId"
        :initial-company-id="company?.id ?? null"
        @saved="handleOppSaved"
        @cancel="oppDialogOpen = false"
      />
    </v-dialog>

    <v-snackbar v-model="oppSaveSuccess" color="success" timeout="3000">
      {{ oppSuccessMessage }}
      <template #actions>
        <v-btn variant="text" @click="oppSaveSuccess = false">{{ t('common.cancel') }}</v-btn>
      </template>
    </v-snackbar>

    <v-dialog v-model="taskDialogOpen" max-width="min(100%, 760px)" scrollable>
      <CrmTaskRecordDialog
        :task-id="editingTaskId"
        @saved="handleTaskSaved"
        @cancel="taskDialogOpen = false"
      />
    </v-dialog>

    <v-snackbar v-model="taskSaveSuccess" color="success" timeout="3000">
      {{ taskSuccessMessage }}
      <template #actions>
        <v-btn variant="text" @click="taskSaveSuccess = false">{{ t('common.cancel') }}</v-btn>
      </template>
    </v-snackbar>

    <v-dialog v-model="joFormOpen" max-width="min(100%, 760px)" scrollable>
      <JobOrderForm
        v-if="joFormOpen"
        :job="joFormJob"
        @saved="handleJoSaved"
        @cancel="joFormOpen = false"
      />
    </v-dialog>

    <v-snackbar v-model="joSaveSuccess" color="success" timeout="3000">
      {{ joSuccessMessage }}
      <template #actions>
        <v-btn variant="text" @click="joSaveSuccess = false">{{ t('common.cancel') }}</v-btn>
      </template>
    </v-snackbar>

    <!-- Invoice Editor Dialog -->
    <BillingInvoiceEditorDialog
      v-model="invShowEditorDialog"
      :mode="invEditorMode"
      :external-invoice-id="invEditorInvoiceId"
      @saved="handleInvSaved"
    />

    <v-dialog v-model="invShowMarkSentConfirmation" max-width="400">
      <v-card>
        <v-card-title>{{ t('billing.invoices.actions.confirmMarkSent') }}</v-card-title>
        <v-card-text>
          {{ t('billing.invoices.messages.markSentConfirm') }}
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn variant="text" @click="invShowMarkSentConfirmation = false">{{ t('billing.invoices.actions.cancel') }}</v-btn>
          <v-btn color="primary" variant="elevated" :loading="isInvSending" @click="performInvMarkSent">
            {{ t('billing.invoices.actions.markAsSent') }}
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </section>
</template>

<script setup lang="ts">
import { ref, watch, onMounted, computed } from 'vue'
import { useI18n } from 'vue-i18n'
import axios from 'axios'
import { getCrmCompanies, getCrmCompany, getCrmPeople, getCrmOpportunities, getCrmOpportunityStageOptions, getCrmTasks, getCrmTaskStatusOptions, getCrmCompanyTimeline } from '@/services/crm'
import { apiClient } from '@/services/api'
import { getCompanyPaperlessFiles } from '@/services/files'
import { getJobList, deleteJobOrder } from '@/services/jobOrders'
import { getJobDetail } from '@/services/jobs'
import { statusIcon, statusColor, statusLabel } from '@/composables/useJobStatus'
import { getOrderTypeMeta } from '@/utils/orderType'
import { useLocaleFormatters } from '@/composables/useLocaleFormatters'
import { listInvoices, sendInvoice, downloadInvoicePdf, downloadDeliveryNote, type InvoiceBillingSummary } from '@/services/billing'
import CrmCompanyRecordDialog from '@/components/crm/CrmCompanyRecordDialog.vue'
import CrmPeopleRecordDialog from '@/components/crm/CrmPeopleRecordDialog.vue'
import CrmOpportunityRecordDialog from '@/components/crm/CrmOpportunityRecordDialog.vue'
import CrmTaskRecordDialog from '@/components/crm/CrmTaskRecordDialog.vue'
import JobOrderForm from '@/components/forms/JobOrderForm.vue'
import BillingInvoiceEditorDialog from '@/components/billing/BillingInvoiceEditorDialog.vue'
import ListMobileCard, { type ListMobileCardColumn } from '@/components/grids/ListMobileCard.vue'
import { useViewSettings } from '@/composables/useColumnPersistence'
import { useResponsiveList } from '@/composables/useResponsiveList'
import { useGlobalDateFormatter } from '@/composables/useGlobalDateFormatter'
import type { CrmCompany, CrmPerson, CrmOpportunity, CrmTask, CrmTimelineItem, JobOrderRecord, JobDetail, PaperlessNgxDocument } from '@/types/api'

const STORAGE_KEY = 'customer-360-left-pane-width'
const MIN_WIDTH_PX = 280
const MAX_WIDTH_PX = 600

const leftPaneWidth = ref(loadStoredWidth())
const isDragging = ref(false)

function loadStoredWidth(): number {
  const stored = localStorage.getItem(STORAGE_KEY)
  if (stored) {
    const parsed = parseFloat(stored)
    if (!isNaN(parsed)) return Math.max(MIN_WIDTH_PX, Math.min(MAX_WIDTH_PX, parsed))
  }
  return 400
}

onMounted(() => {
  document.documentElement.style.setProperty('--left-pane-width-fallback', leftPaneWidth.value + 'px')
})

function startResize(e: MouseEvent) {
  e.preventDefault()
  isDragging.value = true
}

function onMouseMove(e: MouseEvent) {
  const clamped = Math.max(MIN_WIDTH_PX, Math.min(MAX_WIDTH_PX, e.clientX))
  leftPaneWidth.value = clamped
  document.documentElement.style.setProperty('--left-pane-width-fallback', clamped + 'px')
}

function stopResize() {
  isDragging.value = false
  localStorage.setItem(STORAGE_KEY, String(leftPaneWidth.value))
}

const dialogOpen = ref(false)
const editingCompanyId = ref<string | null>(null)

function openEditDialog() {
  editingCompanyId.value = company.value?.id ?? null
  dialogOpen.value = true
}

function handleSaved(saved: CrmCompany) {
  company.value = saved
  dialogOpen.value = false
}

const personDialogOpen = ref(false)
const editingPersonId = ref<string | null>(null)

function openPersonDialog(personId: string) {
  editingPersonId.value = personId
  personDialogOpen.value = true
}

function handlePersonSaved(saved: CrmPerson) {
  personDialogOpen.value = false
}

const { t } = useI18n({ useScope: 'global' })

const companies = ref<CrmCompany[]>([])
const company = ref<CrmCompany | null>(null)
const people = ref<CrmPerson[]>([])
const selectedCompanyId = ref<string | null>(null)
const companySearch = ref('')
const loadingCompanies = ref(false)
const loadingCompany = ref(false)
const activeTab = ref('job-orders')

async function loadCompanies(lookup?: string) {
  loadingCompanies.value = true
  try {
    const data = await getCrmCompanies({ lookup })
    companies.value = data.sort((a, b) => a.name.localeCompare(b.name))
  } finally {
    loadingCompanies.value = false
  }
}

function onCompanySearch(val: string | null | undefined) {
  companySearch.value = val ?? ''
}

async function onCompanySelected(id: string | null) {
  if (!id) {
    company.value = null
    people.value = []
    return
  }
  loadingCompany.value = true
  try {
    const [c, allPeople] = await Promise.all([
      getCrmCompany(id),
      getCrmPeople(),
    ])
    company.value = c
    const ids = new Set(c.people.map(p => p.id))
    people.value = allPeople.filter(p => ids.has(p.id))
  } finally {
    loadingCompany.value = false
  }
}

watch(companySearch, (val) => {
  loadCompanies(val)
}, { debounce: 300 })

loadCompanies()

function getPerson(id: string): CrmPerson | undefined {
  return people.value.find(p => p.id === id)
}

// --- Timeline tab ---

const timelineItems = ref<CrmTimelineItem[]>([])
const loadingTimeline = ref(false)

async function loadTimeline() {
  if (!company.value) {
    timelineItems.value = []
    return
  }
  loadingTimeline.value = true
  try {
    timelineItems.value = await getCrmCompanyTimeline(company.value.id)
  } catch {
    timelineItems.value = []
  } finally {
    loadingTimeline.value = false
  }
}

watch(company, () => {
  loadTimeline()
})

const { format: timelineFormat } = useGlobalDateFormatter()

function timelineDotColor(type: string): string {
  if (type.startsWith('company.')) return 'primary'
  if (type.startsWith('task.')) return 'warning'
  if (type.startsWith('opportunity.')) return 'success'
  if (type.startsWith('note')) return 'info'
  return 'grey'
}

function timelineIcon(type: string): string {
  if (type.startsWith('company.')) return 'mdi-domain'
  if (type.startsWith('task.')) return 'mdi-format-list-checks'
  if (type.startsWith('opportunity.')) return 'mdi-trending-up'
  if (type.startsWith('note')) return 'mdi-sticky-note-outline'
  return 'mdi-circle-small'
}

// --- Opportunities tab ---

type OppDisplayItem = CrmOpportunity & {
  ln: number
}

const oppRows = ref<CrmOpportunity[]>([])
const loadingOpportunities = ref(false)
const oppLookup = ref('')
const oppErrorMessage = ref('')
const oppStageLabelMap = ref<Record<string, string>>({})
const oppDialogOpen = ref(false)
const editingOpportunityId = ref<string | null>(null)
const oppSaveSuccess = ref(false)
const oppSuccessMessage = ref('')
const oppSelectedIds = ref<string[]>([])

const oppViewSettings = useViewSettings('crm-customer360-opportunities', {
  visibleColumns: ['name', 'stage', 'closeDate', 'amount', 'company', 'pointOfContact', 'owner', 'createdOn', 'createdBy', 'updatedOn', 'updatedBy'],
  sortKey: 'name',
  sortDirection: 'asc',
  checkboxMode: false,
  viewMode: 'detail',
})
const oppVisibleColumnKeys = oppViewSettings.visibleColumns
const oppSortKey = oppViewSettings.sortKey
const oppSortDirection = oppViewSettings.sortDirection
const oppCheckboxMode = oppViewSettings.checkboxMode
const oppViewMode = oppViewSettings.viewMode

const { isPhoneLayout, isColumnVisible: oppIsColumnVisible } = useResponsiveList()
const { format: oppFormat } = useGlobalDateFormatter()
const { format: companyFormat } = useGlobalDateFormatter()

getCrmOpportunityStageOptions().then(opts => {
  oppStageLabelMap.value = Object.fromEntries(opts.map(o => [o.value, o.label]))
}).catch(() => {})

function oppStageLabel(value: string): string {
  return oppStageLabelMap.value[value] || value
}

const isOppCardView = computed(() => oppViewMode.value === 'card')

const allOppHeaders = computed(() => [
  { title: t('crm.opportunities.headers.name'), key: 'name', minWidth: '180px' },
  { title: t('crm.opportunities.headers.stage'), key: 'stage', minWidth: '100px' },
  { title: t('crm.opportunities.headers.closeDate'), key: 'closeDate', minWidth: '135px' },
  { title: t('crm.opportunities.headers.amount'), key: 'amount', minWidth: '120px' },
  { title: t('crm.opportunities.headers.company'), key: 'company', minWidth: '160px' },
  { title: t('crm.opportunities.headers.pointOfContact'), key: 'pointOfContact', minWidth: '160px' },
  { title: t('crm.opportunities.headers.owner'), key: 'owner', minWidth: '140px' },
  { title: t('crm.opportunities.headers.createdOn'), key: 'createdOn', minWidth: '135px' },
  { title: t('crm.opportunities.headers.createdBy'), key: 'createdBy', minWidth: '120px' },
  { title: t('crm.opportunities.headers.updatedOn'), key: 'updatedOn', minWidth: '135px' },
  { title: t('crm.opportunities.headers.updatedBy'), key: 'updatedBy', minWidth: '120px' },
])

const oppHeaders = computed(() =>
  allOppHeaders.value.filter((h) =>
    oppVisibleColumnKeys.value.includes(String(h.key)) &&
    oppIsColumnVisible(String(h.key), {
      hideOnPhone: ['closeDate', 'amount', 'company', 'pointOfContact', 'owner', 'createdOn', 'createdBy', 'updatedOn', 'updatedBy'],
      hideOnTablet: [],
    }),
  ),
)

const oppMobileColumns = computed<ListMobileCardColumn<OppDisplayItem>[]>(() => [
  { key: 'name', label: t('crm.opportunities.headers.name'), section: 'header', emphasis: true },
  { key: 'stage', label: t('crm.opportunities.headers.stage'), section: 'header' },
  { key: 'company', label: t('crm.opportunities.headers.company'), section: 'body' },
  { key: 'owner', label: t('crm.opportunities.headers.owner'), section: 'body' },
  { key: 'createdBy', label: t('crm.opportunities.headers.createdBy'), section: 'footer' },
  {
    key: 'updatedOn',
    label: t('crm.opportunities.headers.updatedOn'),
    section: 'footer',
    formatter: (item) => oppFormat(item.updatedOn),
  },
])

const oppSortableColumns = computed(() =>
  allOppHeaders.value.map((h) => ({ key: String(h.key), title: String(h.title || h.key) })),
)

const oppColumnOptions = computed(() => allOppHeaders.value.map((h) => ({ key: String(h.key), title: String(h.title || h.key) })))

const oppDisplayedRows = computed<OppDisplayItem[]>(() => {
  const key = oppSortKey.value as keyof CrmOpportunity
  const result = [...oppRows.value]

  result.sort((lhs, rhs) => {
    const left = String(lhs[key] ?? '')
    const right = String(rhs[key] ?? '')
    return oppSortDirection.value === 'asc' ? left.localeCompare(right) : right.localeCompare(left)
  })

  return result.map((item, index) => ({
    ...item,
    ln: index + 1,
  }))
})

async function loadOpportunities() {
  if (!company.value) {
    oppRows.value = []
    return
  }
  loadingOpportunities.value = true
  oppErrorMessage.value = ''
  try {
    const all = await getCrmOpportunities(oppLookup.value.trim())
    oppRows.value = all.filter(o => o.companyId === company.value!.id)
  } catch {
    oppErrorMessage.value = t('crm.opportunities.messages.loadFailed')
  } finally {
    loadingOpportunities.value = false
  }
}

watch(company, () => {
  loadOpportunities()
})

async function applyOppLookup() {
  await loadOpportunities()
}

async function refreshOppList() {
  oppLookup.value = ''
  await loadOpportunities()
}

function toggleOppColumn(columnKey: string) {
  if (oppVisibleColumnKeys.value.includes(columnKey)) {
    if (oppVisibleColumnKeys.value.length > 1) {
      oppVisibleColumnKeys.value = oppVisibleColumnKeys.value.filter((key) => key !== columnKey)
    }
    return
  }
  oppVisibleColumnKeys.value = [...oppVisibleColumnKeys.value, columnKey]
}

function setOppViewMode(mode: 'detail' | 'card') {
  oppViewMode.value = mode
}

function handleOppCardCheckbox(id: string) {
  if (oppSelectedIds.value.includes(id)) {
    oppSelectedIds.value = oppSelectedIds.value.filter((pid) => pid !== id)
    return
  }
  oppSelectedIds.value = [...oppSelectedIds.value, id]
}

function onOppMobileCardClick(item: OppDisplayItem) {
  if (oppCheckboxMode.value) {
    handleOppMobileSelect(item, !oppSelectedIds.value.includes(item.id))
    return
  }
  openOpportunityPopup(item.id)
}

function handleOppMobileSelect(item: OppDisplayItem | Record<string, unknown>, selected: boolean) {
  const id = String(item.id ?? '')
  if (!id) return
  if (selected) {
    oppSelectedIds.value = [...new Set([...oppSelectedIds.value, id])]
    return
  }
  oppSelectedIds.value = oppSelectedIds.value.filter((pid) => pid !== id)
}

function openOpportunityPopup(id: string) {
  editingOpportunityId.value = id
  oppDialogOpen.value = true
  oppErrorMessage.value = ''
}

function openNewOpportunity() {
  editingOpportunityId.value = null
  oppDialogOpen.value = true
  oppErrorMessage.value = ''
}

async function handleOppSaved(opportunity: CrmOpportunity) {
  await loadOpportunities()
  oppSelectedIds.value = [opportunity.id]
  editingOpportunityId.value = opportunity.id
  oppSuccessMessage.value = t('crm.opportunities.messages.saveSuccess')
  oppSaveSuccess.value = true
}

// --- Tasks tab ---

type TaskDisplayItem = CrmTask & {
  ln: number
}

const taskRows = ref<CrmTask[]>([])
const loadingTasks = ref(false)
const taskLookup = ref('')
const taskErrorMessage = ref('')
const taskStatusLabelMap = ref<Record<string, string>>({})
const taskDialogOpen = ref(false)
const editingTaskId = ref<string | null>(null)
const taskSaveSuccess = ref(false)
const taskSuccessMessage = ref('')
const taskSelectedIds = ref<string[]>([])

const taskViewSettings = useViewSettings('crm-customer360-tasks', {
  visibleColumns: ['title', 'status', 'body', 'dueDate', 'assignee', 'relations', 'createdOn', 'createdBy', 'updatedOn', 'updatedBy'],
  sortKey: 'title',
  sortDirection: 'asc',
  checkboxMode: false,
  viewMode: 'detail',
})
const taskVisibleColumnKeys = taskViewSettings.visibleColumns
const taskSortKey = taskViewSettings.sortKey
const taskSortDirection = taskViewSettings.sortDirection
const taskCheckboxMode = taskViewSettings.checkboxMode
const taskViewMode = taskViewSettings.viewMode

const { format: taskFormat } = useGlobalDateFormatter()

getCrmTaskStatusOptions().then(opts => {
  taskStatusLabelMap.value = Object.fromEntries(opts.map(o => [o.value, o.label]))
}).catch(() => {})

function taskStatusColor(status: string): string {
  switch (status) {
    case 'COMPLETED': return 'green'
    case 'IN_PROGRESS': return 'info'
    default: return 'default'
  }
}

function taskStatusLabel(status: string): string {
  return taskStatusLabelMap.value[status] || status
}

function taskStripHtml(html: string): string {
  const doc = new DOMParser().parseFromString(html, 'text/html')
  return doc.body.textContent?.trim() || ''
}

const isTaskCardView = computed(() => taskViewMode.value === 'card')

const allTaskHeaders = computed(() => [
  { title: t('crm.tasks.headers.title'), key: 'title', minWidth: '220px' },
  { title: t('crm.tasks.headers.status'), key: 'status', minWidth: '100px' },
  { title: t('crm.tasks.headers.body'), key: 'body', minWidth: '200px' },
  { title: t('crm.tasks.headers.dueDate'), key: 'dueDate', minWidth: '135px' },
  { title: t('crm.tasks.headers.assignee'), key: 'assignee', minWidth: '140px' },
  { title: t('crm.tasks.headers.relations'), key: 'relations', minWidth: '180px' },
  { title: t('crm.tasks.headers.createdOn'), key: 'createdOn', minWidth: '135px' },
  { title: t('crm.tasks.headers.createdBy'), key: 'createdBy', minWidth: '120px' },
  { title: t('crm.tasks.headers.updatedOn'), key: 'updatedOn', minWidth: '135px' },
  { title: t('crm.tasks.headers.updatedBy'), key: 'updatedBy', minWidth: '120px' },
])

const taskHeaders = computed(() =>
  allTaskHeaders.value.filter((h) =>
    taskVisibleColumnKeys.value.includes(String(h.key)) &&
    oppIsColumnVisible(String(h.key), {
      hideOnPhone: ['body', 'dueDate', 'assignee', 'relations', 'createdOn', 'createdBy', 'updatedOn', 'updatedBy'],
      hideOnTablet: [],
    }),
  ),
)

const taskMobileColumns = computed<ListMobileCardColumn<TaskDisplayItem>[]>(() => [
  { key: 'title', label: t('crm.tasks.headers.title'), section: 'header', emphasis: true },
  { key: 'status', label: t('crm.tasks.headers.status'), section: 'header' },
  { key: 'assignee', label: t('crm.tasks.headers.assignee'), section: 'body' },
  { key: 'dueDate', label: t('crm.tasks.headers.dueDate'), section: 'body', formatter: (item) => item.dueDate ? taskFormat(item.dueDate) : '-' },
  { key: 'createdBy', label: t('crm.tasks.headers.createdBy'), section: 'footer' },
  {
    key: 'updatedOn',
    label: t('crm.tasks.headers.updatedOn'),
    section: 'footer',
    formatter: (item) => taskFormat(item.updatedOn),
  },
])

const taskSortableColumns = computed(() =>
  allTaskHeaders.value.map((h) => ({ key: String(h.key), title: String(h.title || h.key) })),
)

const taskColumnOptions = computed(() => allTaskHeaders.value.map((h) => ({ key: String(h.key), title: String(h.title || h.key) })))

const taskDisplayedRows = computed<TaskDisplayItem[]>(() => {
  const key = taskSortKey.value as keyof CrmTask
  const result = [...taskRows.value]

  result.sort((lhs, rhs) => {
    const left = String(lhs[key] ?? '')
    const right = String(rhs[key] ?? '')
    return taskSortDirection.value === 'asc' ? left.localeCompare(right) : right.localeCompare(left)
  })

  return result.map((item, index) => ({
    ...item,
    ln: index + 1,
  }))
})

async function loadTasks() {
  if (!company.value) {
    taskRows.value = []
    return
  }
  loadingTasks.value = true
  taskErrorMessage.value = ''
  try {
    const all = await getCrmTasks(taskLookup.value.trim())
    taskRows.value = all.filter(t => t.relations?.some(r => r.id === company.value!.id))
  } catch {
    taskErrorMessage.value = t('crm.tasks.messages.loadFailed')
  } finally {
    loadingTasks.value = false
  }
}

watch(company, () => {
  loadTasks()
})

async function applyTaskLookup() {
  await loadTasks()
}

async function refreshTaskList() {
  taskLookup.value = ''
  await loadTasks()
}

function toggleTaskColumn(columnKey: string) {
  if (taskVisibleColumnKeys.value.includes(columnKey)) {
    if (taskVisibleColumnKeys.value.length > 1) {
      taskVisibleColumnKeys.value = taskVisibleColumnKeys.value.filter((key) => key !== columnKey)
    }
    return
  }
  taskVisibleColumnKeys.value = [...taskVisibleColumnKeys.value, columnKey]
}

function setTaskViewMode(mode: 'detail' | 'card') {
  taskViewMode.value = mode
}

function handleTaskCardCheckbox(id: string) {
  if (taskSelectedIds.value.includes(id)) {
    taskSelectedIds.value = taskSelectedIds.value.filter((pid) => pid !== id)
    return
  }
  taskSelectedIds.value = [...taskSelectedIds.value, id]
}

function onTaskMobileCardClick(item: TaskDisplayItem) {
  if (taskCheckboxMode.value) {
    handleTaskMobileSelect(item, !taskSelectedIds.value.includes(item.id))
    return
  }
  openTaskPopup(item.id)
}

function handleTaskMobileSelect(item: TaskDisplayItem | Record<string, unknown>, selected: boolean) {
  const id = String(item.id ?? '')
  if (!id) return
  if (selected) {
    taskSelectedIds.value = [...new Set([...taskSelectedIds.value, id])]
    return
  }
  taskSelectedIds.value = taskSelectedIds.value.filter((pid) => pid !== id)
}

function openTaskPopup(id: string) {
  editingTaskId.value = id
  taskDialogOpen.value = true
  taskErrorMessage.value = ''
}

function openNewTask() {
  editingTaskId.value = null
  taskDialogOpen.value = true
  taskErrorMessage.value = ''
}

async function handleTaskSaved(task: CrmTask) {
  await loadTasks()
  taskSelectedIds.value = [task.id]
  editingTaskId.value = task.id
  taskSuccessMessage.value = t('crm.tasks.messages.saveSuccess')
  taskSaveSuccess.value = true
}

// --- Job Orders tab ---

const joRows = ref<JobOrderRecord[]>([])
const loadingJobOrders = ref(false)
const joLookup = ref('')
const joErrorMessage = ref('')
const joSelectedIds = ref<string[]>([])
const joDeleting = ref(false)
const joFormOpen = ref(false)
const joFormJob = ref<JobDetail | null>(null)
const joSaveSuccess = ref(false)
const joSuccessMessage = ref('')

const joViewSettings = useViewSettings('crm-customer360-job-orders', {
  visibleColumns: ['orderType', 'ln', 'orderNumber', 'status', 'orderedOn', 'customerName', 'orderTitle', 'attachProduct', 'customerRef', 'attachCustomer', 'orderedBy', 'productStyle', 'invoiceAmount', 'requiredOn', 'modifiedOn', 'modifiedBy', 'completedOn'],
  sortKey: 'orderNumber',
  sortDirection: 'desc',
  checkboxMode: false,
  viewMode: 'detail',
})
const joVisibleColumnKeys = joViewSettings.visibleColumns
const joSortKey = joViewSettings.sortKey
const joSortDirection = joViewSettings.sortDirection
const joCheckboxMode = joViewSettings.checkboxMode
const joViewMode = joViewSettings.viewMode

const isJoCardView = computed(() => joViewMode.value === 'card')

const { format: joFormat } = useGlobalDateFormatter()
const { formatCurrency: joFormatCurrency } = useLocaleFormatters()

function joStatusIcon(value: number): string {
  return statusIcon(value)
}

function joStatusColor(value: number): string {
  return statusColor(value)
}

function joStatusLabel(value: number): string {
  return statusLabel(value)
}

function joOrderTypeMeta(value: number) {
  return getOrderTypeMeta(value)
}

function joCompositeNumber(row: JobOrderRecord): string {
  return row.jobNumber ? `${row.orderNumber}-${row.jobNumber}` : row.orderNumber
}

const allJoHeaders = computed(() => [
  { title: t('jobOrder.jobList.headers.orderType'), key: 'orderType', width: '52px', sortable: false },
  { title: t('jobOrder.jobList.headers.ln'), key: 'ln', width: '52px', sortable: false },
  { title: t('jobOrder.jobList.headers.order'), key: 'orderNumber', width: '132px' },
  { title: t('jobOrder.jobList.headers.status'), key: 'status', width: '72px', sortable: false },
  { title: t('jobOrder.jobList.headers.orderedOn'), key: 'orderedOn', width: '122px' },
  { title: t('jobOrder.jobList.headers.customer'), key: 'customerName', minWidth: '220px' },
  { title: t('jobOrder.jobList.headers.orderTitle'), key: 'orderTitle', minWidth: '240px' },
  { title: t('jobOrder.jobList.headers.attachProduct'), key: 'attachProduct', width: '72px', sortable: false },
  { title: t('jobOrder.jobList.headers.customerRef'), key: 'customerRef', width: '160px' },
  { title: t('jobOrder.jobList.headers.attachCustomer'), key: 'attachCustomer', width: '72px', sortable: false },
  { title: t('jobOrder.jobList.headers.orderedBy'), key: 'orderedBy', width: '100px' },
  { title: t('jobOrder.jobList.headers.quotation'), key: 'productStyle', width: '120px' },
  { title: t('jobOrder.jobList.headers.invoiceAmount'), key: 'invoiceAmount', width: '132px', align: 'end' as const },
  { title: t('jobOrder.jobList.headers.requiredOn'), key: 'requiredOn', width: '122px' },
  { title: t('jobOrder.jobList.headers.modifiedOn'), key: 'modifiedOn', width: '122px' },
  { title: t('jobOrder.jobList.headers.modifiedBy'), key: 'modifiedBy', width: '100px' },
  { title: t('jobOrder.jobList.headers.completedOn'), key: 'completedOn', width: '122px' },
])

const joHeaders = computed(() =>
  allJoHeaders.value.filter((header) => joVisibleColumnKeys.value.includes(String(header.key))),
)

const allFilesHeaders = computed(() => [
  { title: 'ASN', key: 'archiveSerialNumber', width: '80px' },
  { title: t('customer360.files.correspondent'), key: 'correspondentName', width: '160px' },
  { title: t('customer360.files.title'), key: 'title', minWidth: '240px' },
  { title: t('customer360.files.owner'), key: 'ownerName', width: '120px' },
  { title: t('customer360.files.notes'), key: 'noteCount', width: '60px', align: 'center' as const },
  { title: t('customer360.files.documentType'), key: 'documentTypeName', width: '160px' },
  { title: t('customer360.files.created'), key: 'created', width: '100px' },
  { title: t('customer360.files.pages'), key: 'pageCount', width: '60px', align: 'center' as const },
  { title: t('customer360.files.shared'), key: 'isSharedByRequester', width: '60px', align: 'center' as const },
  { title: '', key: 'actions', width: '48px', sortable: false },
])

const filesHeaders = computed(() =>
  allFilesHeaders.value.filter((header) => filesVisibleColumnKeys.value.includes(String(header.key))),
)

const filesColumnOptions = computed(() =>
  allFilesHeaders.value.map((header) => ({ key: String(header.key), title: String(header.title || header.key) })),
)

const filesSortableColumns = computed(() =>
  allFilesHeaders.value
    .filter((header) => header.sortable !== false && header.key !== 'actions')
    .map((header) => ({ key: String(header.key), title: String(header.title || header.key) })),
)

const filesDisplayedRows = computed(() => {
  let result = [...files.value]

  const search = filesLookup.value.trim().toLowerCase()
  if (search) {
    result = result.filter((row) =>
      Object.values(row).some((val) => {
        if (val === null || val === undefined) return false
        return String(val).toLowerCase().includes(search)
      }),
    )
  }

  const key = (filesSortKey.value ?? 'created') as keyof PaperlessNgxDocument
  const direction = filesSortDirection.value ?? 'desc'

  result.sort((lhs, rhs) => {
    const left = String(lhs[key] ?? '')
    const right = String(rhs[key] ?? '')
    return direction === 'asc' ? left.localeCompare(right) : right.localeCompare(left)
  })

  return result
})

const joSortableColumns = computed(() =>
  allJoHeaders.value
    .filter((header) => header.sortable !== false && header.key !== 'status' && header.key !== 'attachProduct' && header.key !== 'attachCustomer')
    .map((header) => ({ key: String(header.key), title: String(header.title) })),
)

const joColumnOptions = computed(() =>
  allJoHeaders.value.map((header) => ({ key: String(header.key), title: String(header.title) })),
)

const joDisplayedRows = computed(() => {
  let result = [...joRows.value]

  const search = joLookup.value.trim().toLowerCase()
  if (search) {
    result = result.filter(row =>
      Object.values(row).some(val => {
        if (val === null || val === undefined) return false
        return String(val).toLowerCase().includes(search)
      }),
    )
  }

  const key = (joSortKey.value ?? 'orderNumber') as keyof JobOrderRecord
  const direction = joSortDirection.value ?? 'desc'

  result.sort((lhs, rhs) => {
    const left = String(lhs[key] ?? '')
    const right = String(rhs[key] ?? '')
    return direction === 'asc' ? left.localeCompare(right) : right.localeCompare(left)
  })

  return result
})

async function loadJobOrders() {
  if (!company.value) {
    joRows.value = []
    return
  }
  loadingJobOrders.value = true
  joErrorMessage.value = ''
  try {
    joRows.value = await getJobList({ lookup: company.value!.name.trim() || undefined })
  } catch {
    joErrorMessage.value = t('jobOrder.jobList.loadFailed')
  } finally {
    loadingJobOrders.value = false
  }
}

watch(company, () => {
  loadJobOrders()
})

function clearJoLookup() {
  joLookup.value = ''
}

async function applyJoLookup() {
  await loadJobOrders()
}

async function refreshJoList() {
  joLookup.value = ''
  await loadJobOrders()
}

function toggleJoColumn(columnKey: string) {
  if (joVisibleColumnKeys.value.includes(columnKey)) {
    if (joVisibleColumnKeys.value.length > 1) {
      joVisibleColumnKeys.value = joVisibleColumnKeys.value.filter((key) => key !== columnKey)
    }
    return
  }
  joVisibleColumnKeys.value = [...joVisibleColumnKeys.value, columnKey]
}

function setJoViewMode(mode: 'detail' | 'card') {
  joViewMode.value = mode
}

function handleJoCardCheckbox(id: string) {
  if (joSelectedIds.value.includes(id)) {
    joSelectedIds.value = joSelectedIds.value.filter((pid) => pid !== id)
    return
  }
  joSelectedIds.value = [...joSelectedIds.value, id]
}

async function onJoRowClick(event: Event, payload: unknown) {
  const row = payload as { item?: JobOrderRecord | { raw?: JobOrderRecord } }
  const item = row?.item
  const record = item && typeof item === 'object' && 'raw' in item ? item.raw : (item as JobOrderRecord | undefined)
  if (!record) return
  if ((event.target as HTMLElement | null)?.closest('a,button,[role="button"],input,label,.v-selection-control')) return
  if (joCheckboxMode.value) {
    const idx = joSelectedIds.value.indexOf(record.orderId)
    if (idx >= 0) joSelectedIds.value.splice(idx, 1)
    else joSelectedIds.value.push(record.orderId)
    return
  }
  await openJoEditor(record)
}

async function openJoEditor(record: JobOrderRecord) {
  try {
    joFormJob.value = await getJobDetail(record.orderId)
    joFormOpen.value = true
  } catch {
    joErrorMessage.value = t('jobOrder.openEditFailed')
  }
}

function openNewJobOrder() {
  joFormJob.value = null
  joFormOpen.value = true
}

async function confirmJoBatchDelete() {
  const idsToDelete = [...joSelectedIds.value]
  if (idsToDelete.length === 0) return
  if (!window.confirm(t('jobOrder.jobList.batchDeleteConfirm', { count: idsToDelete.length }))) return
  joDeleting.value = true
  let succeeded = 0
  let failed = 0
  for (const id of idsToDelete) {
    try {
      await deleteJobOrder(id)
      succeeded++
    } catch {
      failed++
    }
  }
  joDeleting.value = false
  joSelectedIds.value = []
  await loadJobOrders()
  if (failed > 0) {
    joErrorMessage.value = t('jobOrder.jobList.batchDeleteResult', { succeeded, failed, total: idsToDelete.length })
  }
}

function handleJoSaved() {
  joFormOpen.value = false
  joSaveSuccess.value = true
  loadJobOrders()
}

// --- Invoices tab ---

const invRows = ref<InvoiceBillingSummary[]>([])
const loadingInvoices = ref(false)
const invInvoiceLookup = ref('')
const invErrorMessage = ref('')
const invSelectedIds = ref<string[]>([])
const isInvSending = ref(false)
const invShowMarkSentConfirmation = ref(false)
const invShowEditorDialog = ref(false)
const invEditorMode = ref<'create' | 'edit' | 'view'>('create')
const invEditorInvoiceId = ref<string | undefined>(undefined)

const invViewSettings = useViewSettings('crm-customer360-invoices', {
  visibleColumns: ['invoiceNumber', 'clientName', 'invoiceDate', 'status', 'amount', 'dueDate'],
  sortKey: 'invoiceDate',
  sortDirection: 'desc',
  checkboxMode: false,
  viewMode: 'detail',
})
const invVisibleColumnKeys = invViewSettings.visibleColumns
const invSortKey = invViewSettings.sortKey
const invSortDirection = invViewSettings.sortDirection
const invCheckboxMode = invViewSettings.checkboxMode
const invViewMode = invViewSettings.viewMode

const { format: invFormat } = useGlobalDateFormatter()
const { formatCurrency: invFormatCurrency } = useLocaleFormatters()

const isInvCardView = computed(() => invViewMode.value === 'card')

const allInvHeaders = computed(() => [
  { title: t('billing.invoices.headers.invoice'), key: 'invoiceNumber', minWidth: '180px' },
  { title: t('billing.invoices.headers.client'), key: 'clientName', minWidth: '220px' },
  { title: t('billing.invoices.headers.invoiceDate'), key: 'invoiceDate', width: '130px' },
  { title: t('billing.invoices.headers.status'), key: 'status', width: '140px' },
  { title: t('billing.invoices.headers.amount'), key: 'amount', width: '140px', align: 'end' as const },
  { title: t('billing.invoices.headers.dueDate'), key: 'dueDate', width: '130px' },
])

const invHeaders = computed(() =>
  allInvHeaders.value.filter((header) => invVisibleColumnKeys.value.includes(String(header.key))),
)

const invColumnOptions = computed(() =>
  allInvHeaders.value.map((header) => ({ key: String(header.key), title: String(header.title || header.key) })),
)

const invSortableColumns = computed(() =>
  allInvHeaders.value.map((header) => ({ key: String(header.key), title: String(header.title || header.key) })),
)

const isInvMarkSentEnabled = computed(() => {
  if (!invCheckboxMode.value || invSelectedIds.value.length !== 1) return false
  const selectedId = invSelectedIds.value[0]
  const selectedInvoice = invRows.value.find((inv) => inv.externalInvoiceId === selectedId)
  return selectedInvoice?.status === 'Draft'
})

const isInvDownloadEnabled = computed(() => {
  if (!invCheckboxMode.value || invSelectedIds.value.length !== 1) return false
  return true
})

const invDisplayedRows = computed(() => {
  const result = [...invRows.value]
  const activeSortKey = invSortKey.value || 'invoiceDate'
  const direction = invSortDirection.value === 'asc' ? 1 : -1

  result.sort((left, right) => compareInv(left, right, activeSortKey) * direction)
  return result
})

async function loadInvoices() {
  if (!company.value) {
    invRows.value = []
    return
  }
  loadingInvoices.value = true
  invErrorMessage.value = ''
  try {
    const lookup = company.value!.name.trim()
    invRows.value = await listInvoices(lookup || undefined, invInvoiceLookup.value.trim() || undefined)
  } catch (e) {
    console.error('Failed to load invoices', e)
    if (axios.isAxiosError<{ message?: string }>(e)) {
      invErrorMessage.value = e.response?.data?.message || e.message || t('billing.invoices.messages.loadFailed')
    } else if (e instanceof Error) {
      invErrorMessage.value = e.message || t('billing.invoices.messages.loadFailed')
    } else {
      invErrorMessage.value = t('billing.invoices.messages.loadFailed')
    }
  } finally {
    loadingInvoices.value = false
  }
}

watch(company, () => {
  loadInvoices()
})

async function applyInvLookup() {
  await loadInvoices()
}

async function refreshInvList() {
  invInvoiceLookup.value = ''
  await loadInvoices()
}

function toggleInvColumn(columnKey: string) {
  if (invVisibleColumnKeys.value.includes(columnKey)) {
    if (invVisibleColumnKeys.value.length > 1) {
      invVisibleColumnKeys.value = invVisibleColumnKeys.value.filter((key) => key !== columnKey)
    }
    return
  }
  invVisibleColumnKeys.value = [...invVisibleColumnKeys.value, columnKey]
}

function setInvViewMode(mode: 'detail' | 'card') {
  invViewMode.value = mode
}

function handleInvCardCheckbox(externalInvoiceId: string) {
  if (invSelectedIds.value.includes(externalInvoiceId)) {
    invSelectedIds.value = invSelectedIds.value.filter((id) => id !== externalInvoiceId)
    return
  }
  invSelectedIds.value = [...invSelectedIds.value, externalInvoiceId]
}

function onInvRowClick(_event: Event, payload: { item: InvoiceBillingSummary }) {
  if (invCheckboxMode.value) return
  openInvoice(payload.item)
}

function openInvoice(invoice: InvoiceBillingSummary) {
  invEditorMode.value = invoice.status === 'Draft' ? 'edit' : 'view'
  invEditorInvoiceId.value = invoice.externalInvoiceId
  invShowEditorDialog.value = true
}

function openNewInvoice() {
  invEditorMode.value = 'create'
  invEditorInvoiceId.value = undefined
  invShowEditorDialog.value = true
}

async function handleInvSaved() {
  await loadInvoices()
}

function handleInvMarkSent() {
  invShowMarkSentConfirmation.value = true
}

async function performInvMarkSent() {
  const selectedId = invSelectedIds.value[0]
  if (!selectedId) return

  isInvSending.value = true
  invErrorMessage.value = ''

  try {
    const updatedSummary = await sendInvoice(selectedId)
    const invoiceIndex = invRows.value.findIndex((inv) => inv.externalInvoiceId === selectedId)
    if (invoiceIndex !== -1) {
      invRows.value[invoiceIndex] = updatedSummary
    }
    invSelectedIds.value = []
    invShowMarkSentConfirmation.value = false
  } catch (e) {
    console.error('Failed to send invoice', e)
    if (axios.isAxiosError<{ message?: string }>(e)) {
      invErrorMessage.value = e.response?.data?.message || e.message || t('billing.invoices.messages.sendFailed')
    } else if (e instanceof Error) {
      invErrorMessage.value = e.message || t('billing.invoices.messages.sendFailed')
    } else {
      invErrorMessage.value = t('billing.invoices.messages.sendUnexpected')
    }
  } finally {
    isInvSending.value = false
  }
}

function invOpenPdfPreviewWindow() {
  const previewWindow = window.open('', '_blank')
  if (!previewWindow) return null
  previewWindow.document.title = t('billing.invoices.messages.previewTitle')
  previewWindow.document.body.innerHTML = `<p style="font-family: sans-serif; padding: 16px;">${t('billing.invoices.messages.previewLoading')}</p>`
  return previewWindow
}

function invShowPdfPreview(previewWindow: Window, blob: Blob) {
  const previewUrl = URL.createObjectURL(blob)
  previewWindow.location.href = previewUrl
  window.setTimeout(() => URL.revokeObjectURL(previewUrl), 60_000)
}

async function handleInvDownloadInvoicePdf() {
  const selectedId = invSelectedIds.value[0]
  if (!selectedId) return

  const previewWindow = invOpenPdfPreviewWindow()
  if (!previewWindow) {
    invErrorMessage.value = t('billing.invoices.messages.previewBlocked')
    return
  }

  invErrorMessage.value = ''

  try {
    const blob = await downloadInvoicePdf(selectedId)
    invShowPdfPreview(previewWindow, blob)
  } catch (e) {
    previewWindow.close()
    console.error('Failed to download invoice PDF', e)
    if (axios.isAxiosError<{ message?: string }>(e)) {
      invErrorMessage.value = e.response?.data?.message || e.message || t('billing.invoices.messages.downloadInvoicePdfFailed')
    } else if (e instanceof Error) {
      invErrorMessage.value = e.message || t('billing.invoices.messages.downloadInvoicePdfFailed')
    } else {
      invErrorMessage.value = t('billing.invoices.messages.downloadInvoicePdfUnexpected')
    }
  }
}

async function handleInvDownloadDeliveryNote() {
  const selectedId = invSelectedIds.value[0]
  if (!selectedId) return

  const previewWindow = invOpenPdfPreviewWindow()
  if (!previewWindow) {
    invErrorMessage.value = t('billing.invoices.messages.previewBlocked')
    return
  }

  invErrorMessage.value = ''

  try {
    const blob = await downloadDeliveryNote(selectedId)
    invShowPdfPreview(previewWindow, blob)
  } catch (e) {
    previewWindow.close()
    console.error('Failed to download delivery note', e)
    if (axios.isAxiosError<{ message?: string }>(e)) {
      invErrorMessage.value = e.response?.data?.message || e.message || t('billing.invoices.messages.downloadDeliveryNoteFailed')
    } else if (e instanceof Error) {
      invErrorMessage.value = e.message || t('billing.invoices.messages.downloadDeliveryNoteFailed')
    } else {
      invErrorMessage.value = t('billing.invoices.messages.downloadDeliveryNoteUnexpected')
    }
  }
}

function invDisplayValue(value?: string | null) {
  return value || t('billing.invoices.labels.empty')
}

function invStatusLabel(status?: string | null) {
  if (!status) return t('billing.invoices.status.unknown')
  const normalized = status.trim().toLowerCase()
  if (normalized === 'draft') return t('billing.invoices.status.draft')
  if (normalized === 'sent') return t('billing.invoices.status.sent')
  if (normalized === 'viewed') return t('billing.invoices.status.viewed')
  if (normalized === 'partial') return t('billing.invoices.status.partial')
  if (normalized === 'paid') return t('billing.invoices.status.paid')
  if (normalized === 'cancelled') return t('billing.invoices.status.cancelled')
  if (normalized === 'reversed') return t('billing.invoices.status.reversed')
  if (normalized === 'overdue') return t('billing.invoices.status.overdue')
  if (normalized === 'unpaid') return t('billing.invoices.status.unpaid')
  if (normalized === 'deleted') return t('billing.invoices.status.deleted')
  return status
}

function invStatusColor(status: string) {
  const normalized = status.toLowerCase()
  if (normalized.includes('paid')) return 'success'
  if (normalized.includes('overdue')) return 'error'
  if (normalized.includes('sent') || normalized.includes('view')) return 'info'
  if (normalized === 'cancelled' || normalized === 'reversed' || normalized.includes('deleted')) return 'default'
  return 'warning'
}

function compareInv(left: InvoiceBillingSummary, right: InvoiceBillingSummary, key: string) {
  switch (key) {
    case 'amount':
      return left.amount - right.amount
    case 'invoiceDate':
      return compareInvDateValues(left.invoiceDate, right.invoiceDate)
    case 'dueDate':
      return compareInvDateValues(left.dueDate, right.dueDate)
    case 'lastSyncedAt':
      return compareInvDateValues(left.lastSyncedAt, right.lastSyncedAt)
    case 'invoiceNumber':
      return left.invoiceNumber.localeCompare(right.invoiceNumber)
    case 'clientName':
      return left.clientName.localeCompare(right.clientName)
    case 'status':
      return left.status.localeCompare(right.status)
    default:
      return 0
  }
}

function compareInvDateValues(left?: string, right?: string) {
  const leftValue = left ? new Date(left).getTime() : Number.NEGATIVE_INFINITY
  const rightValue = right ? new Date(right).getTime() : Number.NEGATIVE_INFINITY
  return leftValue - rightValue
}

// --- Files tab ---

const files = ref<PaperlessNgxDocument[]>([])
const loadingFiles = ref(false)
const filesErrorMessage = ref('')
const filesLookup = ref('')
const filesSelectedIds = ref<number[]>([])
const formatFileDate = useGlobalDateFormatter().format

const filesViewSettings = useViewSettings('crm-customer360-files', {
  visibleColumns: ['archiveSerialNumber', 'correspondentName', 'title', 'ownerName', 'noteCount', 'documentTypeName', 'created', 'pageCount', 'isSharedByRequester', 'actions'],
  sortKey: 'created',
  sortDirection: 'desc',
  checkboxMode: false,
  viewMode: 'detail',
})
const filesVisibleColumnKeys = filesViewSettings.visibleColumns
const filesSortKey = filesViewSettings.sortKey
const filesSortDirection = filesViewSettings.sortDirection
const filesCheckboxMode = filesViewSettings.checkboxMode
const filesViewMode = filesViewSettings.viewMode

const isFilesCardView = computed(() => filesViewMode.value === 'card')

function setFilesViewMode(mode: 'detail' | 'card') {
  filesViewMode.value = mode
}

async function loadFiles() {
  if (!company.value) {
    files.value = []
    return
  }
  loadingFiles.value = true
  filesErrorMessage.value = ''
  try {
    const result = await getCompanyPaperlessFiles(company.value.id, company.value.name.trim())
    files.value = result.documents
  } catch (e) {
    console.error('[FilesTab]', e)
    filesErrorMessage.value = t('customer360.files.loadFailed')
  } finally {
    loadingFiles.value = false
  }
}

watch(company, () => {
  loadFiles()
})

function clearFilesLookup() {
  filesLookup.value = ''
}

async function applyFilesLookup() {
  await loadFiles()
}

async function refreshFilesList() {
  filesLookup.value = ''
  await loadFiles()
}

function toggleFilesColumn(columnKey: string) {
  if (filesVisibleColumnKeys.value.includes(columnKey)) {
    if (filesVisibleColumnKeys.value.length > 1) {
      filesVisibleColumnKeys.value = filesVisibleColumnKeys.value.filter((key) => key !== columnKey)
    }
    return
  }
  filesVisibleColumnKeys.value = [...filesVisibleColumnKeys.value, columnKey]
}

function openFile(doc: PaperlessNgxDocument) {
  const previewWindow = window.open('', '_blank')
  if (!previewWindow) return

  previewWindow.document.title = 'Loading...'
  previewWindow.document.body.innerHTML = '<p style="font-family: sans-serif; padding: 16px;">Loading...</p>'

  apiClient.get(`/api/v2/crm/files/${doc.id}/download`, { responseType: 'blob' })
    .then((response) => {
      const url = URL.createObjectURL(response.data)
      previewWindow.location.href = url
      setTimeout(() => URL.revokeObjectURL(url), 60_000)
    })
    .catch(() => {
      previewWindow.document.body.innerHTML = '<p style="font-family: sans-serif; padding: 16px;">Failed to load document.</p>'
    })
}

function fileIcon(mimeType: string | null | undefined): string {
  if (!mimeType) return 'mdi-file-outline'
  if (mimeType.startsWith('image/')) return 'mdi-file-image'
  if (mimeType === 'application/pdf') return 'mdi-file-pdf-box'
  if (mimeType.startsWith('text/')) return 'mdi-file-document-outline'
  if (mimeType.includes('spreadsheet') || mimeType.includes('excel')) return 'mdi-file-excel'
  if (mimeType.includes('presentation') || mimeType.includes('powerpoint')) return 'mdi-file-powerpoint'
  return 'mdi-file-outline'
}

function fileIconColor(mimeType: string | null | undefined): string {
  if (!mimeType) return 'grey'
  if (mimeType.startsWith('image/')) return 'success'
  if (mimeType === 'application/pdf') return 'error'
  if (mimeType.startsWith('text/')) return 'info'
  if (mimeType.includes('spreadsheet') || mimeType.includes('excel')) return 'green'
  if (mimeType.includes('presentation') || mimeType.includes('powerpoint')) return 'orange'
  return 'grey'
}
</script>

<style scoped>
.customer-360-page {
  display: flex;
  height: calc(100vh - 7rem);
  position: relative;
}

.customer-360-page.is-dragging {
  cursor: col-resize;
  user-select: none;
}

.resize-overlay {
  position: fixed;
  inset: 0;
  z-index: 9999;
  cursor: col-resize;
}

.left-pane {
  width: var(--left-pane-width-fallback, 400px);
  flex-shrink: 0;
  display: flex;
  flex-direction: column;
}

.splitter {
  width: 4px;
  flex-shrink: 0;
  cursor: col-resize;
  background: transparent;
  transition: background 0.15s;
  margin: 0 2px;
  border-radius: 2px;
}

.splitter:hover,
.is-dragging .splitter {
  background: rgb(var(--v-theme-primary));
}

.right-pane {
  flex: 1;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.company-select-card {
  flex: 1;
}

.detail-tabs-card {
  flex: 1;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.detail-tabs-card :deep(.v-tabs-window) {
  flex: 1;
  overflow-y: auto;
}

.company-info {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.company-info-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.5rem;
}

.edit-btn {
  opacity: 0.6;
  transition: opacity 0.15s;
}

.edit-btn:hover {
  opacity: 1;
}

.people-section {
  display: flex;
  flex-direction: column;
  gap: 0.4rem;
}

.people-label {
  display: flex;
  align-items: center;
  gap: 0.25rem;
}

.people-cards {
  display: flex;
  flex-direction: column;
  gap: 0.3rem;
}

.person-card {
  border: 1px solid rgba(var(--v-theme-on-surface), 0.12);
  border-radius: 8px;
  padding: 0.4rem 0.6rem;
}

.person-card-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.25rem;
}

.person-card-body {
  display: flex;
  flex-direction: column;
  gap: 0.15rem;
  margin-top: 0.25rem;
}

.person-detail {
  display: flex;
  align-items: center;
  gap: 0.25rem;
}

.info-row {
  display: flex;
  align-items: center;
  gap: 0.25rem;
}

.tab-content {
  padding: 1.5rem;
}

.opportunities-tab-content {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.opportunities-tab-content .filter-bar {
  display: grid;
  gap: 12px;
  grid-template-columns: minmax(200px, 1fr) auto auto;
  align-items: center;
}

.opportunities-tab-content .toolbar-bar {
  display: flex;
  gap: 8px;
  align-items: center;
  flex-wrap: wrap;
}

.opportunities-tab-content .toolbar-menu-list {
  max-height: 340px;
  overflow: auto;
}

.opportunities-tab-content .opportunities-table {
  border-top-left-radius: 8px;
  border-top-right-radius: 8px;
  overflow: hidden;
}

.opportunities-tab-content .opportunities-table :deep(.v-table__wrapper > table > thead > tr > th),
.opportunities-tab-content .opportunities-table :deep(.v-data-table__th) {
  white-space: nowrap;
  background-color: color-mix(in srgb, rgb(var(--v-theme-surface-variant)) 88%, rgb(var(--v-theme-primary)) 12%) !important;
  color: rgb(var(--v-theme-on-surface-variant)) !important;
}

.opportunities-tab-content .opportunities-table :deep(.v-table__wrapper > table > thead > tr > th:first-child),
.opportunities-tab-content .opportunities-table :deep(.v-data-table__th:first-child) {
  border-top-left-radius: 8px;
}

.opportunities-tab-content .opportunities-table :deep(.v-table__wrapper > table > thead > tr > th:last-child),
.opportunities-tab-content .opportunities-table :deep(.v-data-table__th:last-child) {
  border-top-right-radius: 8px;
}

.opportunity-card-list {
  display: grid;
  gap: 0.9rem;
  grid-template-columns: 1fr;
}

@media (min-width: 960px) {
  .opportunity-card-list {
    grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
    align-items: start;
  }
}

.opportunity-card {
  display: grid;
  grid-template-columns: 1fr auto;
  gap: 0.8rem;
  padding: 1rem;
  border: 1px solid rgba(var(--v-theme-primary), 0.12);
  background: rgba(255, 255, 255, 0.72);
}

.opportunity-card__checkbox {
  grid-column: 2;
  grid-row: 1;
  align-self: start;
  justify-self: end;
}

.opportunity-card__header {
  grid-column: 1;
  grid-row: 1;
}

.opportunity-card__body,
.opportunity-card__footer {
  grid-column: 1 / -1;
}

.opportunity-card__header,
.opportunity-card__footer {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 0.75rem;
}

.opportunity-card__body {
  display: grid;
  gap: 0.45rem;
}

@media (max-width: 960px) {
  .opportunities-tab-content .filter-bar {
    grid-template-columns: 1fr;
  }
}

.tasks-tab-content {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.tasks-tab-content .filter-bar {
  display: grid;
  gap: 12px;
  grid-template-columns: minmax(200px, 1fr) auto auto;
  align-items: center;
}

.tasks-tab-content .toolbar-bar {
  display: flex;
  gap: 8px;
  align-items: center;
  flex-wrap: wrap;
}

.tasks-tab-content .toolbar-menu-list {
  max-height: 340px;
  overflow: auto;
}

.tasks-tab-content .tasks-table {
  border-top-left-radius: 8px;
  border-top-right-radius: 8px;
  overflow: hidden;
}

.tasks-tab-content .tasks-table :deep(.v-table__wrapper > table > thead > tr > th),
.tasks-tab-content .tasks-table :deep(.v-data-table__th) {
  white-space: nowrap;
  background-color: color-mix(in srgb, rgb(var(--v-theme-surface-variant)) 88%, rgb(var(--v-theme-primary)) 12%) !important;
  color: rgb(var(--v-theme-on-surface-variant)) !important;
}

.tasks-tab-content .tasks-table :deep(.v-table__wrapper > table > thead > tr > th:first-child),
.tasks-tab-content .tasks-table :deep(.v-data-table__th:first-child) {
  border-top-left-radius: 8px;
}

.tasks-tab-content .tasks-table :deep(.v-table__wrapper > table > thead > tr > th:last-child),
.tasks-tab-content .tasks-table :deep(.v-data-table__th:last-child) {
  border-top-right-radius: 8px;
}

.task-card-list {
  display: grid;
  gap: 0.9rem;
  grid-template-columns: 1fr;
}

@media (min-width: 960px) {
  .task-card-list {
    grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
    align-items: start;
  }
}

.task-card {
  display: grid;
  grid-template-columns: 1fr auto;
  gap: 0.8rem;
  padding: 1rem;
  border: 1px solid rgba(var(--v-theme-primary), 0.12);
  background: rgba(255, 255, 255, 0.72);
}

.task-card__checkbox {
  grid-column: 2;
  grid-row: 1;
  align-self: start;
  justify-self: end;
}

.task-card__header {
  grid-column: 1;
  grid-row: 1;
}

.task-card__body,
.task-card__footer {
  grid-column: 1 / -1;
}

.task-card__header,
.task-card__footer {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 0.75rem;
}

.task-card__body {
  display: grid;
  gap: 0.45rem;
}

@media (max-width: 960px) {
  .tasks-tab-content .filter-bar {
    grid-template-columns: 1fr;
  }
}

.job-orders-tab-content {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.job-orders-tab-content .filter-bar {
  display: grid;
  gap: 12px;
  grid-template-columns: minmax(200px, 1fr) auto auto;
  align-items: center;
}

.job-orders-tab-content .toolbar-bar {
  display: flex;
  gap: 8px;
  align-items: center;
  flex-wrap: wrap;
}

.job-orders-tab-content .toolbar-menu-list {
  max-height: 340px;
  overflow: auto;
}

.job-orders-tab-content .job-orders-table {
  border-top-left-radius: 8px;
  border-top-right-radius: 8px;
  overflow: hidden;
}

.job-orders-tab-content .job-orders-table :deep(.v-table__wrapper > table > thead > tr > th),
.job-orders-tab-content .job-orders-table :deep(.v-data-table__th) {
  white-space: nowrap;
  background-color: color-mix(in srgb, rgb(var(--v-theme-surface-variant)) 88%, rgb(var(--v-theme-primary)) 12%) !important;
  color: rgb(var(--v-theme-on-surface-variant)) !important;
}

.job-orders-tab-content .job-orders-table :deep(.v-table__wrapper > table > thead > tr > th:first-child),
.job-orders-tab-content .job-orders-table :deep(.v-data-table__th:first-child) {
  border-top-left-radius: 8px;
}

.job-orders-tab-content .job-orders-table :deep(.v-table__wrapper > table > thead > tr > th:last-child),
.job-orders-tab-content .job-orders-table :deep(.v-data-table__th:last-child) {
  border-top-right-radius: 8px;
}

@media (max-width: 960px) {
  .job-orders-tab-content .filter-bar {
    grid-template-columns: 1fr;
  }
}

.jo-card-list {
  display: grid;
  gap: 0.9rem;
  grid-template-columns: 1fr;
}

@media (min-width: 960px) {
  .jo-card-list {
    grid-template-columns: repeat(auto-fill, minmax(320px, 1fr));
    align-items: start;
  }
}

.jo-card {
  display: grid;
  grid-template-columns: 1fr auto;
  gap: 0.8rem;
  padding: 1rem;
  border: 1px solid rgba(var(--v-theme-primary), 0.12);
  background: rgba(255, 255, 255, 0.72);
  cursor: pointer;
}

.jo-card__checkbox {
  grid-column: 2;
  grid-row: 1;
  align-self: start;
  justify-self: end;
}

.jo-card__header {
  grid-column: 1;
  grid-row: 1;
}

.jo-card__body,
.jo-card__footer,
.jo-card__meta {
  grid-column: 1 / -1;
}

.jo-card__header,
.jo-card__footer,
.jo-card__meta {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 0.75rem;
}

.jo-card__body {
  display: grid;
  gap: 0.45rem;
}

.jo-card__metrics {
  display: grid;
  gap: 0.3rem;
}

.invoices-tab-content {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.invoices-tab-content .filter-bar {
  display: grid;
  gap: 12px;
  grid-template-columns: minmax(200px, 1fr) auto auto;
  align-items: center;
}

.invoices-tab-content .toolbar-bar {
  display: flex;
  gap: 8px;
  align-items: center;
  flex-wrap: wrap;
}

.invoices-tab-content .toolbar-menu-list {
  max-height: 340px;
  overflow: auto;
}

.invoices-tab-content .invoices-table {
  border-top-left-radius: 8px;
  border-top-right-radius: 8px;
  overflow: hidden;
}

.invoices-tab-content .invoices-table :deep(.v-table__wrapper > table > thead > tr > th),
.invoices-tab-content .invoices-table :deep(.v-data-table__th) {
  white-space: nowrap;
  background-color: color-mix(in srgb, rgb(var(--v-theme-surface-variant)) 88%, rgb(var(--v-theme-primary)) 12%) !important;
  color: rgb(var(--v-theme-on-surface-variant)) !important;
}

.invoices-tab-content .invoices-table :deep(.v-table__wrapper > table > thead > tr > th:first-child),
.invoices-tab-content .invoices-table :deep(.v-data-table__th:first-child) {
  border-top-left-radius: 8px;
}

.invoices-tab-content .invoices-table :deep(.v-table__wrapper > table > thead > tr > th:last-child),
.invoices-tab-content .invoices-table :deep(.v-data-table__th:last-child) {
  border-top-right-radius: 8px;
}

.invoices-tab-content .invoice-card-list {
  display: grid;
  gap: 0.9rem;
  grid-template-columns: 1fr;
}

.invoices-tab-content .invoice-card {
  position: relative;
  display: grid;
  gap: 0.8rem;
  padding: 1rem;
  border: 1px solid rgba(var(--v-theme-primary), 0.12);
  background: rgba(255, 255, 255, 0.72);
  cursor: pointer;
  overflow: hidden;
}

.invoices-tab-content .invoice-card__checkbox-anchor {
  position: absolute;
  top: 0.35rem;
  right: 0.35rem;
  z-index: 1;
  display: flex;
  align-items: flex-start;
  justify-content: flex-end;
}

.invoices-tab-content .invoice-card__checkbox {
  margin: 0;
}

.invoices-tab-content .invoice-card__header,
.invoices-tab-content .invoice-card__body,
.invoices-tab-content .invoice-card__footer {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.75rem;
}

.invoices-tab-content .invoice-card__body,
.invoices-tab-content .invoice-card__footer {
  flex-wrap: wrap;
}

@media (max-width: 960px) {
  .invoices-tab-content .filter-bar {
    grid-template-columns: 1fr;
  }
}

@media (min-width: 960px) {
  .invoices-tab-content .invoice-card-list {
    grid-template-columns: repeat(auto-fill, minmax(320px, 1fr));
    align-items: start;
  }
}

.files-tab-content {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.files-tab-content .filter-bar {
  display: grid;
  gap: 12px;
  grid-template-columns: minmax(200px, 1fr) auto auto;
  align-items: center;
}

.files-tab-content .toolbar-bar {
  display: flex;
  gap: 8px;
  align-items: center;
  flex-wrap: wrap;
}

.files-tab-content .toolbar-menu-list {
  max-height: 340px;
  overflow: auto;
}

.files-tab-content .files-table {
  border-top-left-radius: 8px;
  border-top-right-radius: 8px;
  overflow: hidden;
  border: 1px solid rgba(var(--v-theme-primary), 0.12);
}

.files-tab-content .files-table :deep(.v-table__wrapper > table > thead > tr > th),
.files-tab-content .files-table :deep(.v-data-table__th) {
  font-size: 0.75rem;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.05em;
  white-space: nowrap;
  background-color: color-mix(in srgb, rgb(var(--v-theme-surface-variant)) 88%, rgb(var(--v-theme-primary)) 12%) !important;
  color: rgb(var(--v-theme-on-surface-variant)) !important;
}

.files-tab-content .files-table :deep(.v-table__wrapper > table > thead > tr > th:first-child),
.files-tab-content .files-table :deep(.v-data-table__th:first-child) {
  border-top-left-radius: 8px;
  padding-left: 1rem;
}

.files-tab-content .files-table :deep(.v-table__wrapper > table > thead > tr > th:last-child),
.files-tab-content .files-table :deep(.v-data-table__th:last-child) {
  border-top-right-radius: 8px;
  padding-right: 0.5rem;
}

.files-tab-content .file-card-list {
  display: grid;
  gap: 0.9rem;
  grid-template-columns: 1fr;
}

@media (min-width: 960px) {
  .files-tab-content .file-card-list {
    grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
    align-items: start;
  }
}

.files-tab-content .file-card {
  display: grid;
  grid-template-columns: 1fr auto;
  gap: 0.6rem;
  border: 1px solid rgba(var(--v-theme-primary), 0.12);
  background: rgba(255, 255, 255, 0.72);
  overflow: hidden;
}

.files-tab-content .file-card__thumbnail {
  grid-column: 1 / -1;
  position: relative;
  aspect-ratio: 210 / 297;
  overflow: hidden;
  background-color: rgb(var(--v-theme-surface-variant));
  background-size: contain;
  background-position: center;
  background-repeat: no-repeat;
  border-bottom: 1px solid rgba(var(--v-theme-on-surface), 0.08);
}

.files-tab-content .file-card__thumbnail-fallback {
  position: absolute;
  inset: 0;
  display: flex;
  align-items: center;
  justify-content: center;
}

.files-tab-content .file-card__thumbnail-open-btn {
  position: absolute;
  top: 6px;
  right: 6px;
  z-index: 2;
  background: rgb(255, 255, 255);
  border-radius: 50%;
  box-shadow: 0 1px 4px rgba(0, 0, 0, 0.2);
  transition: transform 0.15s, background 0.15s;
}

.files-tab-content .file-card__thumbnail-open-btn:hover {
  background: rgba(255, 0, 0, 0.1) !important;
  transform: scale(1.15);
}

.files-tab-content .file-card__header {
  grid-column: 1;
  padding-left: 1rem;
  padding-bottom: 0;
  min-width: 0;
}

.files-tab-content .file-card__tags {
  grid-column: 1 / -1;
  display: flex;
  flex-wrap: wrap;
  gap: 4px;
  padding: 0 1rem;
}

.files-tab-content .file-card__body {
  grid-column: 1 / -1;
  display: grid;
  gap: 0.3rem;
  padding: 0 1rem 0.8rem;
}

.files-tab-content .file-card__body-row {
  display: flex;
  gap: 0.4rem;
  align-items: center;
}

.files-tab-content .file-card__footer {
  grid-column: 2;
  align-self: start;
  justify-self: end;
  padding-right: 0.5rem;
  padding-top: 0.2rem;
}

.timeline-tab-content {
  max-height: calc(100vh - 14rem);
  overflow-y: auto;
}

.timeline-tab-content .timeline-card {
  border-color: rgba(var(--v-theme-on-surface), 0.12);
}

.timeline-tab-content .timeline-card :deep(.v-card-text) {
  white-space: pre-line;
}

.timeline-tab-content .timeline-opposite {
  text-align: right;
}
</style>
