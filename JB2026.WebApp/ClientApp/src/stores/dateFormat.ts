// src/stores/dateFormat.ts
import { defineStore } from 'pinia'
import { DATE_FORMATS, formatDate, type DateFormatType } from '@/utils/dateFormatter'

export const useDateFormatStore = defineStore('dateFormat', {
  state: () => ({
    currentFormat: DATE_FORMATS.SHORT_DATE as DateFormatType,
  }),
  
  getters: {
    getCurrentFormat: (state) => state.currentFormat,
  },
  
  actions: {
    setCurrentFormat(format: DateFormatType) {
      this.currentFormat = format
    },
    
    // Method to get formatted date using current global setting
    format(value: string | Date | null | undefined, locale: string = 'en-US'): string {
      return formatDate(value, this.currentFormat, locale)
    }
  }
})
