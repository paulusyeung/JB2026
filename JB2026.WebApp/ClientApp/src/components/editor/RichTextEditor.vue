<template>
  <v-card rounded="xl" elevation="0" class="panel-card">
    <v-card-title>
      <h3 class="text-h6 mb-1">CKEditor 5 Preview</h3>
      <p class="text-body-2 text-medium-emphasis mb-0">Classic build with the minimum toolbar required by the Phase 6 spec.</p>
    </v-card-title>
    <v-card-text>
      <ckeditor :editor="editor" v-model="content" :config="config" />
      <v-divider class="my-4" />
      <h4 class="text-subtitle-1 mb-2">Rendered HTML preview</h4>
      <div class="editor-preview" v-html="content" />
    </v-card-text>
  </v-card>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { Ckeditor } from '@ckeditor/ckeditor5-vue'
import ClassicEditor from '@ckeditor/ckeditor5-build-classic'

const ckeditor = Ckeditor
const editor = ClassicEditor

const content = ref(`
  <h2>Legacy CKEditor 4 content sample</h2>
  <p><strong>Bold</strong>, <em>italic</em>, links, lists, and tables render in the Phase 6 baseline.</p>
  <ul><li>Bullet one</li><li>Bullet two</li></ul>
  <table><tbody><tr><td>Size</td><td>Large</td></tr></tbody></table>
`)

const config = {
  toolbar: ['undo', 'redo', '|', 'bold', 'italic', 'link', '|', 'bulletedList', 'numberedList', '|', 'insertTable'],
}
</script>