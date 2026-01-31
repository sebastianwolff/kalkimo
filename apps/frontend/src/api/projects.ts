import { apiClient, getErrorMessage } from './client';
import type { Project, CalculationResult } from '@/stores/types';

export interface ProjectSummary {
  id: string;
  name: string;
  description?: string;
  version: number;
  createdAt: string;
  updatedAt: string;
  status: string;
}

export interface CreateProjectRequest {
  name: string;
  description?: string;
  currency?: string;
}

export interface ChangesetOperation {
  op: 'add' | 'remove' | 'replace' | 'move' | 'copy' | 'test';
  path: string;
  value?: unknown;
  from?: string;
}

export interface ApplyChangesetRequest {
  operations: ChangesetOperation[];
  description?: string;
}

// Projects API functions
export const projectsApi = {
  async list(): Promise<ProjectSummary[]> {
    try {
      const response = await apiClient.get<ProjectSummary[]>('/projects');
      return response.data;
    } catch (error) {
      throw new Error(getErrorMessage(error));
    }
  },

  async get(id: string): Promise<Project> {
    try {
      const response = await apiClient.get<Project>(`/projects/${id}`);
      return response.data;
    } catch (error) {
      throw new Error(getErrorMessage(error));
    }
  },

  async create(request: CreateProjectRequest): Promise<Project> {
    try {
      const response = await apiClient.post<Project>('/projects', request);
      return response.data;
    } catch (error) {
      throw new Error(getErrorMessage(error));
    }
  },

  async update(id: string, project: Partial<Project>): Promise<Project> {
    try {
      const response = await apiClient.put<Project>(`/projects/${id}`, project);
      return response.data;
    } catch (error) {
      throw new Error(getErrorMessage(error));
    }
  },

  async delete(id: string): Promise<void> {
    try {
      await apiClient.delete(`/projects/${id}`);
    } catch (error) {
      throw new Error(getErrorMessage(error));
    }
  },

  async applyChangeset(id: string, changeset: ApplyChangesetRequest): Promise<Project> {
    try {
      const response = await apiClient.post<Project>(`/projects/${id}/events`, changeset);
      return response.data;
    } catch (error) {
      throw new Error(getErrorMessage(error));
    }
  },

  async calculate(id: string): Promise<CalculationResult> {
    try {
      const response = await apiClient.post<CalculationResult>(`/projects/${id}/calculate`);
      return response.data;
    } catch (error) {
      throw new Error(getErrorMessage(error));
    }
  },

  async saveData(id: string, projectData: Project): Promise<void> {
    try {
      await apiClient.put(`/projects/${id}/data`, projectData);
    } catch (error) {
      throw new Error(getErrorMessage(error));
    }
  },

  async getData(id: string): Promise<Project> {
    try {
      const response = await apiClient.get<Project>(`/projects/${id}/data`);
      return response.data;
    } catch (error) {
      throw new Error(getErrorMessage(error));
    }
  },

  async export(id: string, format: 'pdf' | 'csv' = 'pdf'): Promise<Blob> {
    try {
      const response = await apiClient.get(`/projects/${id}/export`, {
        params: { format },
        responseType: 'blob'
      });
      return response.data;
    } catch (error) {
      throw new Error(getErrorMessage(error));
    }
  }
};
