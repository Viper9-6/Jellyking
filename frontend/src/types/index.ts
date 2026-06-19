export interface ServiceStatus {
  id: string
  serviceId: string
  name: string
  basePath: string
  icon: string
  priority: number
  host: string
  port: number
  isUp: boolean
  downReason: string | null
  authType: 'none' | 'apikey' | 'jellyfin' | 'qbittorrent'
  lastChecked: string
}

export interface MeDto {
  id: string
  username: string
  role: 'Admin' | 'User'
}

export interface SetupRequest {
  username: string
  password: string
}

export interface LoginRequest {
  username: string
  password: string
}

export interface Service {
  id: string
  slug: string
  name: string
  host: string
  port: number
  basePath: string
  healthPath: string
  icon: string
  webSocketPaths: string
  priority: number
  enabled: boolean
  authType: 'none' | 'apikey' | 'jellyfin' | 'qbittorrent'
}

export interface UserDto {
  id: string
  username: string
  role: 'Admin' | 'User'
  createdAt: string
  updatedAt: string
}

export interface CreateUserRequest {
  username: string
  password: string
  role: 'Admin' | 'User'
}

export interface UpdateUserRequest {
  username?: string
  password?: string
  role?: 'Admin' | 'User'
}

export interface SettingsDto {
  title: string
  theme: string
  localAccessEnabled: boolean
}

export interface UpdateSettingsRequest {
  title?: string
  theme?: string
  localAccessEnabled?: boolean
}

export interface ServiceTestRequest {
  host: string
  port: number
  basePath: string
  healthPath: string
}

export interface ServiceTestResult {
  host: string
  port: number
  healthPath: string
  tcpOk: boolean
  reachable: boolean
  httpStatus: number | null
  hint: string
}

// Editable form state used by the Add/Edit modals: service fields plus the
// write-only credential material (secret for apikey/jellyfin, user+pass for qbit).
export type ServiceFormState = Omit<Service, 'id'> & {
  secret: string
  username: string
  password: string
}
