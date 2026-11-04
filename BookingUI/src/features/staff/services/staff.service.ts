import { apiClient } from '../../../lib/api-client'
import type { InviteStaffRequest, PagedStaff, StaffStatus } from '../types/staff.types'

export const staffService = {
  getStaff(pageNumber = 1, pageSize = 20) {
    return apiClient<PagedStaff>(`/api/auth/staff?pageNumber=${pageNumber}&pageSize=${pageSize}`)
  },
  invite(request: InviteStaffRequest) {
    return apiClient<{ invitationToken: string }>('/api/auth/staff/invitations', {
      method: 'POST',
      body: JSON.stringify(request),
    })
  },
  updateStatus(userId: number, status: StaffStatus) {
    return apiClient<void>(`/api/auth/staff/${userId}/status`, {
      method: 'PATCH',
      body: JSON.stringify({ status }),
    })
  },
  updateRole(userId: number, roleCode: string) {
    return apiClient<void>(`/api/auth/staff/${userId}/role`, {
      method: 'PATCH',
      body: JSON.stringify({ roleCode }),
    })
  },
}
