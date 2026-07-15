export const crmMessages = {
  staffMember: {
    form: {
      newTitle: 'New Staff Member',
      editTitle: 'Staff Member',
      email: 'Email',
      invalidEmail: 'Invalid email format',
      emailInUse: 'This email is already in use by another staff member',
    },
    actions: {
      new: 'New Staff Member',
    },
  },
  companies: {
    lookup: 'Search companies...',
    headers: {
      name: 'Company Name',
      accountOwner: 'Account Owner',
      domainName: 'Domain Name',
      address: 'Address',
      peopleCount: 'People',
      opportunitiesCount: 'Opportunities',
      createdOn: 'Created on',
      createdBy: 'Created by',
      updatedOn: 'Updated on',
      updatedBy: 'Updated by',
    },
    actions: {
      columns: 'Columns',
      sorting: 'Sorting',
      sortBy: 'Sort by',
      asc: 'A-Z',
      desc: 'Z-A',
      checkbox: 'Checkbox',
      views: 'Views',
      detailView: 'Detail View',
      cardView: 'Card View',
      selected: '{count} selected',
    },
    messages: {
      loadFailed: 'Failed to load companies',
    },
  },
} as const
