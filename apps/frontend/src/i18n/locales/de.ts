export default {
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
    redo: 'Wiederholen'
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
    about: 'Über'
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
      summary: 'Zusammenfassung'
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
    units: 'Einheiten',
    addUnit: 'Einheit hinzufügen',
    weg: {
      title: 'WEG-Konfiguration',
      meaPerMille: 'Miteigentumsanteil (MEA)',
      totalMea: 'Gesamt-MEA',
      sharePercent: 'Anteil in %',
      reserveBalance: 'Rücklagenstand'
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
    nonTransferable: 'Nicht umlagefähig'
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
      taxClassification: 'Steuerliche Behandlung'
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
      Other: 'Sonstiges'
    },
    taxClassifications: {
      MaintenanceExpense: 'Erhaltungsaufwand',
      AcquisitionCost: 'Anschaffungskosten (AfA)',
      ImprovementCost: 'Herstellungskosten (AfA)',
      NotDeductible: 'Nicht absetzbar'
    },
    totalCapex: 'Gesamtinvestitionen'
  },

  summary: {
    title: 'Zusammenfassung',
    projectOverview: 'Projektübersicht',
    keyMetrics: 'Kennzahlen',
    calculate: 'Berechnen',
    recalculate: 'Neu berechnen',
    export: 'Exportieren',
    cashflowTable: 'Cashflow-Übersicht',
    taxSummary: 'Steuerliche Zusammenfassung',
    warnings: 'Hinweise & Warnungen',
    returnMetrics: 'Renditekennzahlen',
    bankMetrics: 'Bankkennzahlen',
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
      noi: 'NOI',
      debtService: 'Schuldendienst',
      capex: 'Investitionen',
      beforeTax: 'CF vor Steuern',
      tax: 'Steuern',
      afterTax: 'CF nach Steuern',
      cumulative: 'Kumuliert',
      debt: 'Restschuld'
    },
    tax: {
      depreciationRate: 'AfA-Satz',
      depreciationBasis: 'Bemessungsgrundlage',
      annualDepreciation: 'Jährliche AfA',
      totalDepreciation: 'Gesamte AfA',
      totalInterest: 'Gesamte Schuldzinsen',
      totalMaintenance: 'Gesamter Erhaltungsaufwand',
      totalTax: 'Gesamte Steuerlast',
      rule15: '15%-Regel',
      rule15Triggered: '15%-Regel ausgelöst',
      rule15NotTriggered: '15%-Regel nicht ausgelöst',
      rule15Amount: 'Erhaltungsaufwand in 3 Jahren'
    },
    totals: {
      totalEquity: 'Eingesetztes Eigenkapital',
      totalCashflowBefore: 'Gesamter Cashflow vor Steuern',
      totalCashflowAfter: 'Gesamter Cashflow nach Steuern'
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
    contact: 'Kontakt'
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
