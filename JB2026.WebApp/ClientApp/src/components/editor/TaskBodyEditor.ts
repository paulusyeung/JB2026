import ClassicEditor from '@ckeditor/ckeditor5-editor-classic/src/classiceditor'
import '@ckeditor/ckeditor5-theme-lark/theme/theme.css'
import Essentials from '@ckeditor/ckeditor5-essentials/src/essentials'
import Paragraph from '@ckeditor/ckeditor5-paragraph/src/paragraph'
import Bold from '@ckeditor/ckeditor5-basic-styles/src/bold'
import Italic from '@ckeditor/ckeditor5-basic-styles/src/italic'
import Underline from '@ckeditor/ckeditor5-basic-styles/src/underline'
import Strikethrough from '@ckeditor/ckeditor5-basic-styles/src/strikethrough'
import Link from '@ckeditor/ckeditor5-link/src/link'
import List from '@ckeditor/ckeditor5-list/src/list'
import BlockQuote from '@ckeditor/ckeditor5-block-quote/src/blockquote'
import Undo from '@ckeditor/ckeditor5-undo/src/undo'
import Font from '@ckeditor/ckeditor5-font/src/font'

ClassicEditor.builtinPlugins = [
  Essentials,
  Paragraph,
  Bold,
  Italic,
  Underline,
  Strikethrough,
  Link,
  List,
  BlockQuote,
  Undo,
  Font,
]

ClassicEditor.defaultConfig = {
  toolbar: {
    items: [
      'undo', 'redo', '|',
      'fontSize', 'fontFamily', 'fontColor', 'fontBackgroundColor', '|',
      'bold', 'italic', 'underline', 'strikethrough', 'link', '|',
      'bulletedList', 'numberedList', '|',
      'blockQuote',
    ],
    shouldNotGroupWhenFull: true,
  },
  fontSize: {
    options: [9, 11, 13, 'default', 17, 19, 21, 24, 28, 36, 48],
    supportAllValues: true,
  },
  fontFamily: {
    options: [
      'default',
      'Arial, Helvetica, sans-serif',
      'Courier New, Courier, monospace',
      'Georgia, serif',
      'Lucida Sans Unicode, Lucida Grande, sans-serif',
      'Tahoma, Geneva, sans-serif',
      'Times New Roman, Times, serif',
      'Trebuchet MS, Helvetica, sans-serif',
      'Verdana, Geneva, sans-serif',
    ],
    supportAllValues: true,
  },
  licenseKey: 'GPL',
}

export default ClassicEditor
