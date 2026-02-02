import helpTexts from './helpTexts.de.json';

export default {
  helpTexts,
  common: {
    appName: 'Kalkimo Planner',
    save: 'Speichern',
    cancel: 'Abbrechen',
    delete: 'Löschen',
    edit: 'Bearbeiten',
    add: 'Hinzufügen',
    remove: 'Entfernen',
    close: 'Schließen',
    back: 'Zurück',
    next: 'Weiter',
    finish: 'Abschließen',
    loading: 'Laden...',
    error: 'Fehler',
    success: 'Erfolgreich',
    warning: 'Warnung',
    info: 'Information',
    yes: 'Ja',
    no: 'Nein',
    required: 'Pflichtfeld',
    optional: 'Optional',
    currency: 'Währung',
    year: 'Jahr',
    month: 'Monat',
    years: 'Jahre',
    months: 'Monate',
    percent: 'Prozent',
    amount: 'Betrag',
    total: 'Gesamt',
    perMonth: 'pro Monat',
    perYear: 'pro Jahr',
    sqm: 'm²',
    undo: 'Rückgängig',
    redo: 'Wiederholen',
    calculate: 'Berechnen',
    closeProject: 'Projekt schließen'
  },

  auth: {
    login: 'Anmelden',
    logout: 'Abmelden',
    register: 'Registrieren',
    name: 'Name',
    email: 'E-Mail',
    password: 'Passwort',
    passwordConfirm: 'Passwort bestätigen',
    confirmPassword: 'Passwort bestätigen',
    forgotPassword: 'Passwort vergessen?',
    loginError: 'Anmeldung fehlgeschlagen',
    registerError: 'Registrierung fehlgeschlagen',
    invalidCredentials: 'Ungültige Anmeldedaten'
  },

  nav: {
    dashboard: 'Dashboard',
    projects: 'Projekte',
    newProject: 'Neues Projekt',
    settings: 'Einstellungen',
    help: 'Hilfe',
    about: 'Über',
    admin: 'Administration'
  },

  wizard: {
    title: 'Projekt-Assistent',
    progress: 'Fortschritt',
    step: 'Schritt',
    of: 'von',
    steps: {
      project: 'Projekt',
      property: 'Objekt',
      purchase: 'Kauf',
      financing: 'Finanzierung',
      rent: 'Mieten',
      costs: 'Kosten',
      tax: 'Steuern',
      capex: 'Maßnahmen',
      summary: 'Ergebnis'
    }
  },

  project: {
    title: 'Projekt-Grunddaten',
    name: 'Projektname',
    namePlaceholder: 'z.B. Mehrfamilienhaus Hamburg',
    description: 'Beschreibung',
    descriptionPlaceholder: 'Optionale Notizen zum Projekt',
    currency: 'Währung',
    period: 'Analysezeitraum',
    startDate: 'Startdatum',
    endDate: 'Enddatum',
    createdAt: 'Erstellt am',
    updatedAt: 'Zuletzt geändert'
  },

  property: {
    title: 'Objektdaten',
    type: 'Objekttyp',
    types: {
      SingleFamily: 'Einfamilienhaus (EFH)',
      MultiFamily: 'Mehrfamilienhaus (MFH)',
      Condominium: 'Eigentumswohnung (ETW)',
      Commercial: 'Gewerbeimmobilie',
      Mixed: 'Gemischt genutzt'
    },
    constructionYear: 'Baujahr',
    condition: 'Zustand',
    conditions: {
      New: 'Neubau',
      Good: 'Gut',
      Fair: 'Befriedigend',
      Poor: 'Sanierungsbedürftig',
      NeedsRenovation: 'Kernsanierung nötig'
    },
    totalArea: 'Gesamtfläche',
    livingArea: 'Wohnfläche',
    landArea: 'Grundstücksfläche',
    unitCount: 'Anzahl Einheiten',
    addUnit: 'Einheit hinzufügen',
    weg: {
      title: 'WEG-Konfiguration',
      meaPerMille: 'Miteigentumsanteil (MEA)',
      totalMea: 'Gesamt-MEA',
      sharePercent: 'Anteil in %',
      reserveBalance: 'Rücklagenstand'
    },
    components: {
      title: 'Gebäudebestandteile',
      hint: 'Optional. Ohne Angabe wird das Baujahr des Gebäudes verwendet.',
      lastRenovation: 'Letzte Sanierung',
      expectedCycle: 'Erw. Zyklus (Jahre)',
      condition: 'Zustand',
      categories: {
        Heating: 'Heizung',
        Roof: 'Dach',
        Facade: 'Fassade',
        Windows: 'Fenster',
        Electrical: 'Elektrik',
        Plumbing: 'Sanitär',
        Interior: 'Innenausbau',
        Exterior: 'Außenanlagen',
        Other: 'Sonstiges',
        Energy: 'Energetische Sanierung',
        Kitchen: 'Einbauküche',
        Bathroom: 'Sanitärinstallation',
        UnitRenovation: 'Grundlegende Renovierung',
        UnitOther: 'Sonstiges (Wohnung)'
      }
    },
    units: {
      title: 'Mieteinheiten',
      hint: 'Definieren Sie die einzelnen Mieteinheiten des Objekts.',
      addUnit: 'Einheit hinzufügen',
      name: 'Bezeichnung',
      area: 'Fläche',
      type: 'Typ',
      removeUnit: 'Einheit entfernen',
      components: 'Ausstattung',
      componentsHint: 'Zustand der wohnungsbezogenen Bauteile.',
      types: {
        Residential: 'Wohnung',
        Commercial: 'Gewerbe',
        Parking: 'Stellplatz',
        Storage: 'Lager/Keller'
      }
    },
    unitComponents: {
      title: 'Ausstattung der Mieteinheiten',
      hint: 'Optional. Erfassen Sie den Zustand der wohnungsbezogenen Bauteile pro Einheit.',
    },
    marketReference: {
      title: 'Regionale Marktpreise',
      hint: 'Optional. Ermöglicht den Vergleich des Kaufpreises mit dem regionalen Marktwert.',
      pricePerSqm: 'Regionaler Quadratmeterpreis',
      fairValue: 'Errechneter Marktwert',
      hintEmpty: 'Geben Sie den regionalen qm-Preis ein um den Marktwert zu berechnen'
    }
  },

  purchase: {
    title: 'Kaufdaten',
    price: 'Kaufpreis',
    date: 'Kaufdatum',
    landValuePercent: 'Grundstücksanteil',
    landValueInfo: 'Der Grundstücksanteil ist nicht abschreibbar',
    costs: 'Kaufnebenkosten',
    addCost: 'Position hinzufügen',
    costTypes: {
      notaryFee: 'Notarkosten',
      landTransferTax: 'Grunderwerbsteuer',
      brokerFee: 'Maklergebühr',
      landRegistration: 'Grundbucheintragung',
      other: 'Sonstige'
    },
    totalCosts: 'Gesamte Kaufnebenkosten',
    totalInvestment: 'Gesamtinvestition'
  },

  financing: {
    title: 'Finanzierung',
    equity: 'Eigenkapital',
    loans: 'Darlehen',
    addLoan: 'Darlehen hinzufügen',
    loan: {
      name: 'Bezeichnung',
      principal: 'Darlehensbetrag',
      interestRate: 'Sollzins',
      initialRepayment: 'Anfängliche Tilgung',
      fixedInterestYears: 'Zinsbindung',
      startDate: 'Startdatum',
      specialRepayment: 'Sondertilgung p.a.'
    },
    summary: {
      totalLoan: 'Gesamtdarlehen',
      totalEquity: 'Eigenkapital',
      ltv: 'Beleihungsauslauf (LTV)',
      monthlyRate: 'Monatliche Rate'
    }
  },

  rent: {
    title: 'Mieteinnahmen',
    units: 'Mieteinheiten',
    addUnit: 'Mieteinheit hinzufügen',
    selectUnit: 'Einheit zuordnen',
    unit: {
      name: 'Bezeichnung',
      monthlyRent: 'Kaltmiete',
      serviceCharge: 'Nebenkosten',
      annualIncrease: 'Jährl. Steigerung'
    },
    vacancyRate: 'Leerstandsquote',
    rentLossReserve: 'Mietausfallreserve',
    totalMonthlyRent: 'Monatsmiete gesamt',
    totalAnnualRent: 'Jahresmiete gesamt'
  },

  costs: {
    title: 'Laufende Kosten',
    items: 'Kostenpositionen',
    addItem: 'Position hinzufügen',
    item: {
      name: 'Bezeichnung',
      monthlyAmount: 'Monatlich',
      isTransferable: 'Umlagefähig',
      annualIncrease: 'Jährl. Steigerung'
    },
    categories: {
      administration: 'Verwaltung',
      maintenance: 'Instandhaltung',
      insurance: 'Versicherung',
      landTax: 'Grundsteuer',
      other: 'Sonstige'
    },
    totalMonthly: 'Monatl. Kosten gesamt',
    transferable: 'Davon umlagefähig',
    nonTransferable: 'Nicht umlagefähig',
    maintenanceReserve: {
      title: 'Instandhaltungsrücklage',
      monthlyAmount: 'Rücklage/Monat',
      annualIncrease: 'Jährl. Steigerung',
      hint: 'Reduziert den Cashflow, ist aber steuerlich nicht sofort absetzbar.',
      yearly: 'Rücklage/Jahr',
    },
    totalWithReserve: 'Gesamt inkl. Rücklage'
  },

  tax: {
    title: 'Steuerliche Einstellungen',
    marginalRate: 'Grenzsteuersatz',
    churchTax: 'Kirchensteuer',
    isCorporate: 'Kapitalgesellschaft',
    depreciation: {
      title: 'Abschreibung (AfA)',
      rate: 'AfA-Satz',
      basis: 'Bemessungsgrundlage',
      annual: 'Jährliche AfA',
      info: 'Basierend auf Baujahr und Gebäudewert'
    },
    section82b: {
      title: '§82b EStDV Verteilung',
      enabled: 'Verteilung aktivieren',
      years: 'Verteilungszeitraum',
      info: 'Größere Erhaltungsaufwendungen auf 2-5 Jahre verteilen'
    },
    section23: {
      title: '§23 EStG Spekulationssteuer',
      holdingPeriod: 'Haltefrist',
      info: 'Nach 10 Jahren steuerfrei (Privatpersonen)'
    }
  },

  capex: {
    title: 'Investitionsmaßnahmen',
    measures: 'Geplante Maßnahmen',
    addMeasure: 'Maßnahme hinzufügen',
    measure: {
      name: 'Bezeichnung',
      category: 'Kategorie',
      amount: 'Betrag',
      scheduledDate: 'Geplant für',
      taxClassification: 'Steuerliche Behandlung',
      impact: 'Wirtschaftliche Auswirkung',
      costSavings: 'Betriebskostenersparnis/Monat',
      costSavingsHint: 'Z.B. niedrigere Heizkosten nach energetischer Sanierung',
      rentIncrease: 'Mieterhöhung/Monat',
      rentIncreaseHint: 'Z.B. Modernisierungsumlage oder höhere Miete bei Neuvermietung',
      rentIncreasePercent: 'Mieterhöhung (%)',
      delayMonths: 'Bauzeit (Monate)',
      delayMonthsHint: 'Erst nach Abschluss wirken Einsparungen und Mieterhöhung'
    },
    categories: {
      Roof: 'Dach',
      Facade: 'Fassade',
      Windows: 'Fenster',
      Heating: 'Heizung',
      Electrical: 'Elektrik',
      Plumbing: 'Sanitär',
      Interior: 'Innenausbau',
      Exterior: 'Außenanlagen',
      Other: 'Sonstiges',
      Kitchen: 'Einbauküche',
      Bathroom: 'Sanitärinstallation',
      UnitRenovation: 'Grundlegende Renovierung',
      UnitOther: 'Sonstiges (Wohnung)'
    },
    taxClassifications: {
      MaintenanceExpense: 'Erhaltungsaufwand',
      AcquisitionCost: 'Anschaffungskosten (AfA)',
      ImprovementCost: 'Herstellungskosten (AfA)',
      NotDeductible: 'Nicht absetzbar'
    },
    totalCapex: 'Gesamtinvestitionen',
    suggestMeasures: 'Maßnahmen vorschlagen',
    initialPrompt: {
      title: 'Sanierungsmaßnahmen ermitteln',
      description: 'Basierend auf den erfassten Gebäudebestandteilen und deren Alter können wir automatisch Sanierungsvorschläge mit Kostenprognose erstellen.',
      showSuggestions: 'Vorschläge anzeigen',
      manualOnly: 'Nur manuell erfassen'
    },
    suggestions: {
      title: 'Vorgeschlagene Maßnahmen',
      accept: 'Übernehmen',
      dismiss: 'Ablehnen',
      acceptAll: 'Alle übernehmen',
      reasoning: 'Begründung',
      age: 'Alter',
      cycle: 'Zyklus',
      priority: 'Priorität',
      priorities: {
        Critical: 'Kritisch',
        High: 'Hoch',
        Medium: 'Mittel',
        Low: 'Niedrig'
      },
      empty: 'Keine Maßnahmen im Analysezeitraum empfohlen'
    },
    oneTime: {
      toggle: 'Einmalige Investition',
    },
    recurring: {
      toggle: 'Regelmäßige Maßnahme',
      intervalPercent: 'Intervall (% der Zykluszeit)',
      costPercent: 'Kosten (% der Erneuerungskosten)',
      cycleExtension: 'Zyklusverlängerung (%)',
      calculatedInterval: 'Intervall: {years} Jahre',
      calculatedCost: 'Kosten je Durchführung: {cost}',
      occurrences: '{count}× im Zeitraum: {years}',
      noOccurrences: 'Keine Durchführungen im Analysezeitraum',
    },
    taxHint: {
      beforePurchase: 'Vor dem Erwerb: Kosten werden als Anschaffungskosten eingestuft (§255 HGB).',
      rule15Exceeded: '15%-Grenze überschritten: Erhaltungsaufwendungen ({total}) in 3 Jahren übersteigen 15% des Gebäudewerts ({threshold}) → anschaffungsnahe Herstellungskosten (§6 Abs. 1 Nr. 1a EStG).',
      within3Years: 'Innerhalb der 3-Jahres-Frist nach Erwerb. 15%-Grenze des Gebäudewerts: {threshold} (verbleibend: {remaining}).',
      afterThreeYears: 'Erhaltungsaufwand: Sofort absetzbar oder Verteilung nach §82b EStDV (2–5 Jahre).',
    }
  },

  summary: {
    title: 'Zusammenfassung',
    projectOverview: 'Projektübersicht',
    keyMetrics: 'Kennzahlen',
    calculate: 'Berechnen',
    recalculate: 'Neu berechnen',
    export: 'Exportieren',
    cashflowChart: 'Cashflow-Verlauf',
    cashflowTable: 'Cashflow-Übersicht',
    financingChart: 'Finanzierungsverlauf',
    taxBridge: 'Steuer-Bridge',
    taxSummary: 'Steuerliche Zusammenfassung',
    capexTimeline: 'Maßnahmen-Timeline',
    riskIndicators: 'Risikoindikatoren',
    warnings: 'Hinweise & Warnungen',
    returnMetrics: 'Renditekennzahlen',
    bankMetrics: 'Bankkennzahlen',
    tabs: {
      overview: 'Übersicht',
      cashflow: 'Cashflow',
      tax: 'Steuern',
      property: 'Immobilie',
    },
    metrics: {
      irrBeforeTax: 'IRR vor Steuern',
      irrAfterTax: 'IRR nach Steuern',
      npvBeforeTax: 'NPV vor Steuern',
      npvAfterTax: 'NPV nach Steuern',
      cashOnCash: 'Cash-on-Cash',
      equityMultiple: 'Eigenkapital-Multiple',
      roi: 'Eigenkapitalrendite p.a.',
      dscrMin: 'DSCR (Minimum)',
      dscrAvg: 'DSCR (Durchschnitt)',
      icrMin: 'ICR (Minimum)',
      ltvInitial: 'LTV initial',
      ltvFinal: 'LTV final',
      breakEvenRent: 'Break-Even Miete'
    },
    cashflow: {
      year: 'Jahr',
      grossRent: 'Bruttomiete',
      vacancy: 'Leerstand',
      effectiveRent: 'Nettomiete',
      operatingCosts: 'Betriebskosten',
      maintenanceReserve: 'Rücklage',
      noi: 'NOI',
      debtService: 'Schuldendienst',
      interest: 'Zinsen',
      principal: 'Tilgung',
      capex: 'Investitionen',
      capexNet: 'Invest. (netto)',
      reserveBalance: 'Rücklagenstand',
      beforeTax: 'CF vor Steuern',
      tax: 'Steuern',
      afterTax: 'CF nach Steuern',
      cumulative: 'Kumuliert',
      debt: 'Restschuld',
      ltv: 'LTV',
      dscr: 'DSCR'
    },
    tax: {
      depreciationRate: 'AfA-Satz',
      depreciationBasis: 'Bemessungsgrundlage',
      annualDepreciation: 'Jährliche AfA',
      totalDepreciation: 'Gesamte AfA',
      totalInterest: 'Gesamte Schuldzinsen',
      totalMaintenance: 'Gesamter Erhaltungsaufwand',
      totalOperating: 'Gesamte Betriebskosten',
      totalTax: 'Gesamte Steuerlast',
      totalSavings: 'Steuerersparnis durch Abzüge',
      effectiveRate: 'Effektiver Steuersatz',
      totalMaintenanceReserve: 'Instandhaltungsrücklage (nicht absetzbar)',
      rule15: '15%-Regel',
      rule15Triggered: '15%-Regel ausgelöst',
      rule15NotTriggered: '15%-Regel nicht ausgelöst',
      rule15Amount: 'Erhaltungsaufwand in 3 Jahren',
      bridge: {
        income: 'Mieteinnahmen',
        depreciation: 'AfA',
        interest: 'Schuldzinsen',
        maintenance: 'Erhaltungsaufwand',
        operating: 'Betriebskosten',
        reserve: 'Rücklage (n. absetzb.)',
        taxableIncome: 'Zu verst. Einkommen',
        taxPayment: 'Steuerzahlung'
      }
    },
    risk: {
      maintenance: 'Sanierungsrisiko',
      liquidity: 'Liquiditätsrisiko',
      low: 'Niedrig',
      medium: 'Mittel',
      high: 'Hoch',
      critical: 'Kritisch'
    },
    totals: {
      totalEquity: 'Eingesetztes Eigenkapital',
      totalCashflowBefore: 'Gesamter Cashflow vor Steuern',
      totalCashflowAfter: 'Gesamter Cashflow nach Steuern'
    },
    propertyValue: {
      title: 'Immobilienwertprognose',
      subtitle: 'Geschätzter Marktwert unter Berücksichtigung von Zustand, Investitionen und Marktentwicklung',
      purchasePrice: 'Kaufpreis',
      marketValue: 'Marktwert (reine Preisentwicklung)',
      improvementUplift: 'Wertsteigerung durch Investitionen',
      conditionFactor: 'Zustandsfaktor',
      estimatedValue: 'Geschätzter Wert',
      initialCondition: 'Zustandsfaktor bei Kauf',
      improvementFactor: 'Wertübertragung Investitionen',
      scenario: {
        conservative: 'Konservativ',
        base: 'Basis',
        optimistic: 'Optimistisch',
      },
      appreciation: 'Marktentwicklung p.a.',
      year: 'Jahr',
      finalValue: 'Kalkulierter Verkaufspreis',
      vs: 'vs. Kaufpreis',
      marketAppreciationRow: 'Marktentwicklung',
      conditionAdjustment: 'Zustandsanpassung',
      investmentRow: 'Investitionen',
      meanReversionRow: 'Marktkonvergenz',
      regionalPrice: 'Regionaler qm-Preis',
      fairMarketValue: 'Errechneter Marktwert',
      purchaseVsMarket: 'Kaufpreis vs. Markt',
      assessment: {
        below: 'Unter Marktwert',
        at: 'Marktniveau',
        above: 'Über Marktwert',
      },
      endConditionFactor: 'Zustandsfaktor am Ende',
      explanationTitle: 'Erläuterung',
      driver: {
        initialCondition: 'Gebäude Baujahr {constructionYear}, Zustand {condition}, {componentCount} Bauteile erfasst. Ausgangsfaktor: {factor}%.',
        overdueComponents: '{count} Bauteile überfällig ({names}) — durchschnittlich {avgOverdueYears} Jahre über Erneuerungszyklus. Dies drückt den Zustandsfaktor.',
        degradation: 'Zustandsfaktor sinkt von {startFactor}% auf {endFactor}% über {years} Jahre (−{totalDecline} Prozentpunkte) durch natürliche Alterung.',
        componentDeterioration: '{uncoveredCount} Bauteile werden im Haltezeitraum fällig und sind nicht durch Maßnahmen abgedeckt. Geschätzte Wertminderung: {uncoveredAmount} €.',
        investments: '{measureCount} Maßnahmen geplant ({totalAmount} €). Wertbeitrag: {valueUplift} € (70% Übertragung). Zustandsverbesserung: +{conditionBoost} Prozentpunkte.',
        marketAppreciation: 'Marktentwicklung {rate}% p.a. über {years} Jahre ergibt +{appreciationPercent}% reine Preissteigerung ({appreciationAmount} €).',
        meanReversion: 'Kaufpreis {gapPercent}% {assessment} regionalem Marktwert — {direction} von {adjustmentAmount} € (Halbwertszeit 7 Jahre).',
        summary: 'Geschätzter Wert nach {years} Jahren (Basis-Szenario): {finalValue} € ({changeDirection}{changePercent}% bzw. {changeDirection}{changeAbsolute} € ggü. Kaufpreis).',
      },
      componentDeterioration: {
        title: 'Bauteil-Zustandsentwicklung',
        subtitle: 'Zustandsverschlechterung durch fällige Erneuerungen im Haltezeitraum',
        component: 'Bauteil',
        age: 'Alter (Ende)',
        cycleYears: 'Zyklus',
        dueYear: 'Fällig',
        renewalCost: 'Erneuerungskosten',
        valueImpact: 'Werteinfluss',
        statusAtEnd: 'Status',
        totalUncovered: 'Nicht abgedeckte Wertminderung',
        totalCovered: 'Durch Maßnahmen abgedeckt',
        statusLabels: {
          OK: 'OK',
          Overdue: 'Überfällig',
          OverdueAtPurchase: 'Überf. bei Kauf',
          Renewed: 'Erneuert',
        },
        pricedIn: 'eingepreist',
        pricedInHint: 'Der Erneuerungsbedarf war beim Kauf bereits bekannt und ist im Kaufpreis berücksichtigt — daher kein zusätzlicher Wertabschlag.',
        yearsShort: 'J.',
        recurringExplanation: 'Regelmäßig: {name} alle {interval} J. ({count}× im Zeitraum, je {cost}, gesamt {total}). Eff. Zyklus: {effectiveCycle} J. (+{extensionPercent}%). Wertverbesserung: +{improvement}',
        recurringLabel: 'Regelmäßig',
      },
    },
    exit: {
      title: 'Ertragsprognose bei Verkauf',
      subtitle: 'Gesamtrentabilität über die Haltedauer inkl. Veräußerung',
      holdingPeriod: 'Haltedauer',
      years: 'Jahre',
      speculationPeriod: 'Spekulationsfrist (§23 EStG)',
      withinPeriod: 'Innerhalb der 10-Jahres-Frist',
      outsidePeriod: 'Außerhalb der Frist (steuerfrei)',
      pnlTitle: 'Gewinn- und Verlustrechnung (Laufzeit)',
      totalGrossIncome: 'Gesamte Mieteinnahmen',
      totalOperatingCosts: 'Betriebskosten',
      totalDebtService: 'Schuldendienst',
      totalCapex: 'Investitionsmaßnahmen',
      totalTaxPaid: 'Einkommensteuer',
      totalMaintenanceReserve: 'Instandhaltungsrücklage',
      finalReserveBalance: 'Rücklagenstand bei Verkauf',
      netCashflow: 'Netto-Cashflow (nach Steuern)',
      scenarioTitle: 'Szenarien bei Verkauf',
      scenario: {
        conservative: 'Konservativ',
        base: 'Basis',
        optimistic: 'Optimistisch',
      },
      appreciation: 'Wertsteigerung p.a.',
      propertyValue: 'Immobilienwert bei Verkauf',
      saleCosts: 'Verkaufsnebenkosten',
      capitalGain: 'Veräußerungsgewinn',
      capitalGainsTax: 'Spekulationssteuer',
      outstandingDebt: 'Restschuld',
      netSaleProceeds: 'Netto-Verkaufserlös',
      plusCashflow: 'Kumulierter Cashflow',
      minusEquity: 'Eingesetztes Eigenkapital',
      totalReturn: 'Gesamtertrag',
      totalReturnPercent: 'Gesamtrendite',
      annualizedReturn: 'Rendite p.a.',
      noTax: 'steuerfrei',
    }
  },

  results: {
    title: 'Ergebnisse',
    cashflow: 'Cashflow',
    liquidity: 'Liquidität',
    taxBridge: 'Steuer-Bridge',
    charts: {
      monthly: 'Monatlich',
      yearly: 'Jährlich',
      cumulative: 'Kumuliert'
    }
  },

  help: {
    title: 'Hilfe',
    glossary: 'Glossar',
    faq: 'Häufige Fragen',
    contact: 'Kontakt',
    relatedTopics: 'Verwandte Themen',
    gettingStarted: 'Erste Schritte',
    welcome: 'Willkommen bei Kalkimo Planner, dem professionellen Immobilien-Investitionsrechner.',
    howToStart: 'So starten Sie:',
    step1: 'Klicken Sie auf "Neues Projekt" um ein Investitionsprojekt anzulegen',
    step2: 'Folgen Sie dem Assistenten durch alle Eingabeschritte',
    step3: 'Sehen Sie Ihre Kennzahlen und Cashflow-Prognosen',
    step4: 'Exportieren Sie Berichte für Ihre Bank oder Steuerberater',
    contactText: 'Bei Fragen oder Problemen kontaktieren Sie uns:',
    contactEmail: 'support@kalkimo.de',
    faqTopics: {
      afa: 'tax.depreciation',
      rule15: 'summary.tax.rule15',
      section23: 'tax.section23',
      section82b: 'tax.section82b'
    }
  },

  admin: {
    title: 'Administration',
    users: 'Benutzer',
    userCount: 'Registrierte Benutzer',
    projectCount: 'Projekte gesamt',
    userDetail: 'Benutzer-Details',
    userInfo: 'Benutzer-Informationen',
    roles: 'Rollen',
    registeredAt: 'Registriert am',
    projects: 'Projekte',
    noProjects: 'Keine Projekte vorhanden',
    backToUsers: 'Zurück zur Übersicht'
  },

  errors: {
    required: 'Dieses Feld ist erforderlich',
    invalidEmail: 'Ungültige E-Mail-Adresse',
    minLength: 'Mindestens {min} Zeichen erforderlich',
    maxLength: 'Maximal {max} Zeichen erlaubt',
    minValue: 'Mindestwert: {min}',
    maxValue: 'Maximalwert: {max}',
    positiveNumber: 'Muss eine positive Zahl sein',
    invalidDate: 'Ungültiges Datum',
    networkError: 'Netzwerkfehler. Bitte prüfen Sie Ihre Verbindung.',
    serverError: 'Serverfehler. Bitte versuchen Sie es später erneut.',
    unknownError: 'Ein unbekannter Fehler ist aufgetreten.'
  }
};
