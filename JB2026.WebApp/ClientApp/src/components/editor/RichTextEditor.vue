<template>
  <v-card rounded="xl" elevation="0" class="panel-card">
    <v-card-title>
      <h3 class="text-h6 mb-1">{{ t('editor.title') }}</h3>
      <p class="text-body-2 text-medium-emphasis mb-0">{{ t('editor.subtitle') }}</p>
    </v-card-title>
    <v-card-text>
      <ckeditor :editor="editor" v-model="content" :config="config" />
      <v-divider class="my-4" />
      <h4 class="text-subtitle-1 mb-2">{{ t('editor.previewTitle') }}</h4>
      <div class="editor-preview" v-html="content" />
    </v-card-text>
  </v-card>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { Ckeditor } from '@ckeditor/ckeditor5-vue'
import ClassicEditor from '@ckeditor/ckeditor5-build-classic'

const ckeditor = Ckeditor
const editor = ClassicEditor
const { t } = useI18n({ useScope: 'global' })

const content = ref(t('editor.sampleHtml'))

const config = {
  licenseKey: 'GPL',
  toolbar: {
    items: [
      'undo',
      'redo',
      '|',
      'heading',
      '|',
      'bold',
      'italic',
      'link',
      '|',
      'bulletedList',
      'numberedList',
      '|',
      'outdent',
      'indent',
      '|',
      'blockQuote',
      'insertTable',
      'mediaEmbed',
      'imageUpload',
    ],
    shouldNotGroupWhenFull: true,
  },
}
</script>