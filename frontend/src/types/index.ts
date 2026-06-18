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
