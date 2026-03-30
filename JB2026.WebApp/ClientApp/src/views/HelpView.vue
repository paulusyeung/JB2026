<template>
  <section class="page-section">
    <v-card rounded="xl" elevation="0" class="panel-card">
      <v-card-title class="d-flex align-center ga-3">
        <div>
          <h3 class="text-h6 mb-1">Help center</h3>
          <p class="text-body-2 text-medium-emphasis mb-0">Operational guides and onboarding articles.</p>
        </div>
        <v-spacer />
        <v-btn color="primary" :loading="loading" @click="load">Refresh</v-btn>
      </v-card-title>
      <v-card-text>
        <v-alert v-if="errorMessage" type="warning" variant="tonal" class="mb-3">{{ errorMessage }}</v-alert>

        <v-expansion-panels variant="accordion">
          <v-expansion-panel v-for="article in articles" :key="article.articleId">
            <v-expansion-panel-title>
              {{ article.title }}
              <template #actions>
                <v-chip size="small" variant="tonal" color="secondary">{{ article.category }}</v-chip>
              </template>
            </v-expansion-panel-title>
            <v-expansion-panel-text>{{ article.content }}</v-expansion-panel-text>
          </v-expansion-panel>
        </v-expansion-panels>
      </v-card-text>
    </v-card>
  </section>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { getHelpArticles } from '@/services/help'
import type { HelpArticle } from '@/types/api'

const loading = ref(false)
const errorMessage = ref('')
const articles = ref<HelpArticle[]>([])

onMounted(async () => {
  await load()
})

async function load() {
  loading.value = true
  errorMessage.value = ''
  try {
    articles.value = await getHelpArticles()
  } catch {
    errorMessage.value = 'Unable to load help content. Please verify API availability.'
  } finally {
    loading.value = false
  }
}
</script>