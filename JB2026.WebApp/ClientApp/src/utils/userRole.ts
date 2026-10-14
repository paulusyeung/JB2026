import type { ComposerTranslation } from 'vue-i18n'

export const USER_ROLE = {
  Guest: 0,
  Operator: 1,
  Supervisor: 2,
  Manager: 3,
  Admin: 4,
} as const

export type UserRoleValue = (typeof USER_ROLE)[keyof typeof USER_ROLE]

const USER_ROLE_KEY_BY_VALUE: Record<UserRoleValue, string> = {
  [USER_ROLE.Guest]: 'admin.user.form.roles.guest',
  [USER_ROLE.Operator]: 'admin.user.form.roles.operator',
  [USER_ROLE.Supervisor]: 'admin.user.form.roles.supervisor',
  [USER_ROLE.Manager]: 'admin.user.form.roles.manager',
  [USER_ROLE.Admin]: 'admin.user.form.roles.admin',
}

const USER_ROLE_VALUE_BY_NAME: Record<string, UserRoleValue> = {
  guest: USER_ROLE.Guest,
  operator: USER_ROLE.Operator,
  supervisor: USER_ROLE.Supervisor,
  manager: USER_ROLE.Manager,
  admin: USER_ROLE.Admin,
}

export interface UserRoleOption {
  value: UserRoleValue
  title: string
}

export function buildUserRoleOptions(t: ComposerTranslation): UserRoleOption[] {
  return (Object.entries(USER_ROLE) as [string, UserRoleValue][])
    .sort((lhs, rhs) => lhs[1] - rhs[1])
    .map(([, value]) => ({ value, title: t(USER_ROLE_KEY_BY_VALUE[value]) }))
}

export function formatUserRole(t: ComposerTranslation, role: string | number | null | undefined): string {
  if (role === null || role === undefined || role === '') {
    return '-'
  }

  if (typeof role === 'number') {
    const key = USER_ROLE_KEY_BY_VALUE[role as UserRoleValue]
    return key ? t(key) : String(role)
  }

  const value = USER_ROLE_VALUE_BY_NAME[role.trim().toLowerCase()]
  return value !== undefined ? t(USER_ROLE_KEY_BY_VALUE[value]) : role
}
