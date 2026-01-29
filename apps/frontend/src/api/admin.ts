import { apiClient, getErrorMessage } from './client';

export interface AdminUserSummary {
  id: string;
  email: string;
  name: string;
  roles: string[];
  createdAt: string;
  projectCount: number;
}

export interface AdminProjectMetadata {
  id: string;
  name: string;
  description?: string;
  version: number;
  createdAt: string;
  updatedAt: string;
  ownerId: string;
}

export interface AdminUserDetail {
  id: string;
  email: string;
  name: string;
  roles: string[];
  createdAt: string;
  projects: AdminProjectMetadata[];
}

export const adminApi = {
  async getUsers(): Promise<AdminUserSummary[]> {
    try {
      const response = await apiClient.get<AdminUserSummary[]>('/admin/users');
      return response.data;
    } catch (error) {
      throw new Error(getErrorMessage(error));
    }
  },

  async getUserDetail(userId: string): Promise<AdminUserDetail> {
    try {
      const response = await apiClient.get<AdminUserDetail>(`/admin/users/${userId}`);
      return response.data;
    } catch (error) {
      throw new Error(getErrorMessage(error));
    }
  }
};
