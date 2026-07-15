export const crmMessages = {
  staffMember: {
    form: {
      newTitle: '新增員工',
      editTitle: '員工',
      email: '電子郵件',
      invalidEmail: '電子郵件格式不正確',
      emailInUse: '此電郵已被其他員工使用',
    },
    actions: {
      new: '新增員工',
    },
  },
  companies: {
    lookup: '搜索公司...',
    headers: {
      name: '公司名稱',
      accountOwner: '客戶經理',
      domainName: '域名',
      address: '地址',
      peopleCount: '聯絡人',
      opportunitiesCount: '商機',
      createdOn: '建立時間',
      createdBy: '建立人',
      updatedOn: '更新時間',
      updatedBy: '更新人',
    },
    actions: {
      columns: '列',
      sorting: '排序',
      sortBy: '排序方式',
      asc: 'A-Z',
      desc: 'Z-A',
      checkbox: '複選框',
      views: '檢視',
      detailView: '詳細檢視',
      cardView: '卡片檢視',
      selected: '已選擇 {count} 項',
    },
    messages: {
      loadFailed: '載入公司列表失敗',
    },
  },
} as const
