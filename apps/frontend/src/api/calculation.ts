import { apiClient, getErrorMessage } from './client';
import type { Project, CalculationResult } from '@/stores/types';

/**
 * API-Funktionen für Berechnungen.
 * - calculateAnonymous: Anonyme Berechnung ohne Login (Gast-Modus)
 * - Die authentifizierte Berechnung läuft über projectsApi.calculate(id)
 */
export const calculationApi = {
  /**
   * Anonyme Berechnung — sendet Projektdaten direkt an den Backend-Endpunkt.
   * Kein Login erforderlich. Rate-limited (20 req/min).
   */
  async calculateAnonymous(project: Project): Promise<CalculationResult> {
    try {
      const response = await apiClient.post<CalculationResult>('/calculate', {
        currency: project.currency,
        startPeriod: project.startPeriod,
        endPeriod: project.endPeriod,
        property: project.property,
        purchase: project.purchase,
        financing: project.financing,
        rent: project.rent,
        costs: project.costs,
        taxProfile: project.taxProfile,
        capex: project.capex
      });
      return response.data;
    } catch (error) {
      throw new Error(getErrorMessage(error));
    }
  }
};
