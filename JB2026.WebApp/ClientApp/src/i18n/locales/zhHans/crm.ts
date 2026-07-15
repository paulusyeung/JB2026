export const crmMessages = {
  staffMember: {
    form: {
      newTitle: '新增员工',
      editTitle: '员工',
      email: '邮箱',
      invalidEmail: '邮箱格式不正确',
      emailInUse: '此邮箱已被其他员工使用',
    },
    actions: {
      new: '新增员工',
    },
  },
  companies: {
    lookup: '搜索公司...',
    headers: {
      name: '公司名称',
      accountOwner: '客户经理',
      domainName: '域名',
      address: '地址',
      peopleCount: '联系人',
      opportunitiesCount: '商机',
      createdOn: '创建时间',
      createdBy: '创建人',
      updatedOn: '更新时间',
      updatedBy: '更新人',
    },
    actions: {
      columns: '列',
      sorting: '排序',
      sortBy: '排序方式',
      asc: 'A-Z',
      desc: 'Z-A',
      checkbox: '复选框',
      views: '视图',
      detailView: '详细视图',
      cardView: '卡片视图',
      selected: '已选择 {count} 项',
    },
    messages: {
      loadFailed: '加载公司列表失败',
    },
  },
} as const
