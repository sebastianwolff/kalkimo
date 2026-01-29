export default {
  common: {
    appName: 'Kalkimo Planner',
    save: 'Save',
    cancel: 'Cancel',
    delete: 'Delete',
    edit: 'Edit',
    add: 'Add',
    remove: 'Remove',
    close: 'Close',
    back: 'Back',
    next: 'Next',
    finish: 'Finish',
    loading: 'Loading...',
    error: 'Error',
    success: 'Success',
    warning: 'Warning',
    info: 'Information',
    yes: 'Yes',
    no: 'No',
    required: 'Required',
    optional: 'Optional',
    currency: 'Currency',
    year: 'Year',
    month: 'Month',
    years: 'years',
    months: 'months',
    percent: 'Percent',
    amount: 'Amount',
    total: 'Total',
    perMonth: 'per month',
    perYear: 'per year',
    sqm: 'm²',
    undo: 'Undo',
    redo: 'Redo'
  },

  auth: {
    login: 'Login',
    logout: 'Logout',
    register: 'Register',
    name: 'Name',
    email: 'Email',
    password: 'Password',
    passwordConfirm: 'Confirm Password',
    confirmPassword: 'Confirm Password',
    forgotPassword: 'Forgot Password?',
    loginError: 'Login failed',
    registerError: 'Registration failed',
    invalidCredentials: 'Invalid credentials'
  },

  nav: {
    dashboard: 'Dashboard',
    projects: 'Projects',
    newProject: 'New Project',
    settings: 'Settings',
    help: 'Help',
    about: 'About',
    admin: 'Administration'
  },

  wizard: {
    title: 'Project Wizard',
    progress: 'Progress',
    step: 'Step',
    of: 'of',
    steps: {
      project: 'Project',
      property: 'Property',
      purchase: 'Purchase',
      financing: 'Financing',
      rent: 'Rent',
      costs: 'Costs',
      tax: 'Tax',
      capex: 'CapEx',
      summary: 'Summary'
    }
  },

  project: {
    title: 'Project Details',
    name: 'Project Name',
    namePlaceholder: 'e.g., Multi-family house Hamburg',
    description: 'Description',
    descriptionPlaceholder: 'Optional notes about the project',
    currency: 'Currency',
    period: 'Analysis Period',
    startDate: 'Start Date',
    endDate: 'End Date',
    createdAt: 'Created',
    updatedAt: 'Last Modified'
  },

  property: {
    title: 'Property Details',
    type: 'Property Type',
    types: {
      SingleFamily: 'Single Family Home',
      MultiFamily: 'Multi-family House',
      Condominium: 'Condominium',
      Commercial: 'Commercial Property',
      Mixed: 'Mixed Use'
    },
    constructionYear: 'Construction Year',
    condition: 'Condition',
    conditions: {
      New: 'New Construction',
      Good: 'Good',
      Fair: 'Fair',
      Poor: 'Needs Repair',
      NeedsRenovation: 'Major Renovation Needed'
    },
    totalArea: 'Total Area',
    livingArea: 'Living Area',
    landArea: 'Land Area',
    unitCount: 'Number of Units',
    units: 'Units',
    addUnit: 'Add Unit',
    weg: {
      title: 'WEG Configuration',
      meaPerMille: 'Ownership Share (MEA)',
      totalMea: 'Total MEA',
      sharePercent: 'Share in %',
      reserveBalance: 'Reserve Balance'
    },
    components: {
      title: 'Building Components',
      hint: 'Optional. Construction year is used as default if not specified.',
      lastRenovation: 'Last Renovation',
      expectedCycle: 'Exp. Cycle (Years)',
      condition: 'Condition',
      categories: {
        Heating: 'Heating',
        Roof: 'Roof',
        Facade: 'Facade',
        Windows: 'Windows',
        Electrical: 'Electrical',
        Plumbing: 'Plumbing',
        Interior: 'Interior',
        Energy: 'Energy Renovation'
      }
    }
  },

  purchase: {
    title: 'Purchase Details',
    price: 'Purchase Price',
    date: 'Purchase Date',
    landValuePercent: 'Land Value Portion',
    landValueInfo: 'Land value is not depreciable',
    costs: 'Acquisition Costs',
    addCost: 'Add Cost Item',
    costTypes: {
      notaryFee: 'Notary Fee',
      landTransferTax: 'Land Transfer Tax',
      brokerFee: 'Broker Fee',
      landRegistration: 'Land Registration',
      other: 'Other'
    },
    totalCosts: 'Total Acquisition Costs',
    totalInvestment: 'Total Investment'
  },

  financing: {
    title: 'Financing',
    equity: 'Equity',
    loans: 'Loans',
    addLoan: 'Add Loan',
    loan: {
      name: 'Name',
      principal: 'Principal',
      interestRate: 'Interest Rate',
      initialRepayment: 'Initial Repayment',
      fixedInterestYears: 'Fixed Interest Period',
      startDate: 'Start Date',
      specialRepayment: 'Special Repayment p.a.'
    },
    summary: {
      totalLoan: 'Total Loans',
      totalEquity: 'Total Equity',
      ltv: 'Loan-to-Value (LTV)',
      monthlyRate: 'Monthly Payment'
    }
  },

  rent: {
    title: 'Rental Income',
    units: 'Rental Units',
    addUnit: 'Add Rental Unit',
    unit: {
      name: 'Name',
      monthlyRent: 'Base Rent',
      serviceCharge: 'Service Charge',
      annualIncrease: 'Annual Increase'
    },
    vacancyRate: 'Vacancy Rate',
    rentLossReserve: 'Rent Loss Reserve',
    totalMonthlyRent: 'Total Monthly Rent',
    totalAnnualRent: 'Total Annual Rent'
  },

  costs: {
    title: 'Operating Costs',
    items: 'Cost Items',
    addItem: 'Add Cost Item',
    item: {
      name: 'Name',
      monthlyAmount: 'Monthly',
      isTransferable: 'Recoverable',
      annualIncrease: 'Annual Increase'
    },
    categories: {
      administration: 'Administration',
      maintenance: 'Maintenance',
      insurance: 'Insurance',
      landTax: 'Property Tax',
      other: 'Other'
    },
    totalMonthly: 'Total Monthly Costs',
    transferable: 'Recoverable',
    nonTransferable: 'Non-recoverable'
  },

  tax: {
    title: 'Tax Settings',
    marginalRate: 'Marginal Tax Rate',
    churchTax: 'Church Tax',
    isCorporate: 'Corporation',
    depreciation: {
      title: 'Depreciation',
      rate: 'Depreciation Rate',
      basis: 'Depreciation Basis',
      annual: 'Annual Depreciation',
      info: 'Based on construction year and building value'
    },
    section82b: {
      title: '§82b Distribution',
      enabled: 'Enable Distribution',
      years: 'Distribution Period',
      info: 'Distribute larger maintenance expenses over 2-5 years'
    },
    section23: {
      title: '§23 Capital Gains Tax',
      holdingPeriod: 'Holding Period',
      info: 'Tax-free after 10 years (individuals)'
    }
  },

  capex: {
    title: 'Capital Expenditure',
    measures: 'Planned Measures',
    addMeasure: 'Add Measure',
    measure: {
      name: 'Name',
      category: 'Category',
      amount: 'Amount',
      scheduledDate: 'Scheduled For',
      taxClassification: 'Tax Classification',
      impact: 'Economic Impact',
      costSavings: 'Operating Cost Savings/Month',
      costSavingsHint: 'E.g. lower heating costs after energy renovation',
      rentIncrease: 'Rent Increase/Month',
      rentIncreaseHint: 'E.g. modernization surcharge or higher rent at re-letting',
      rentIncreasePercent: 'Rent Increase (%)',
      delayMonths: 'Construction Period (Months)',
      delayMonthsHint: 'Savings and rent increase take effect only after completion'
    },
    categories: {
      Roof: 'Roof',
      Facade: 'Facade',
      Windows: 'Windows',
      Heating: 'Heating',
      Electrical: 'Electrical',
      Plumbing: 'Plumbing',
      Interior: 'Interior',
      Exterior: 'Exterior',
      Other: 'Other'
    },
    taxClassifications: {
      MaintenanceExpense: 'Maintenance Expense',
      AcquisitionCost: 'Acquisition Cost (Depreciation)',
      ImprovementCost: 'Improvement Cost (Depreciation)',
      NotDeductible: 'Not Deductible'
    },
    totalCapex: 'Total Capital Expenditure',
    suggestMeasures: 'Suggest Measures',
    suggestions: {
      title: 'Suggested Measures',
      accept: 'Accept',
      dismiss: 'Dismiss',
      acceptAll: 'Accept All',
      reasoning: 'Reasoning',
      age: 'Age',
      cycle: 'Cycle',
      priority: 'Priority',
      priorities: {
        Critical: 'Critical',
        High: 'High',
        Medium: 'Medium',
        Low: 'Low'
      },
      empty: 'No measures recommended for the analysis period'
    }
  },

  summary: {
    title: 'Summary',
    projectOverview: 'Project Overview',
    keyMetrics: 'Key Metrics',
    calculate: 'Calculate',
    recalculate: 'Recalculate',
    export: 'Export',
    cashflowChart: 'Cashflow Development',
    cashflowTable: 'Cashflow Overview',
    financingChart: 'Financing Development',
    taxBridge: 'Tax Bridge',
    taxSummary: 'Tax Summary',
    capexTimeline: 'CapEx Timeline',
    riskIndicators: 'Risk Indicators',
    warnings: 'Notes & Warnings',
    returnMetrics: 'Return Metrics',
    bankMetrics: 'Bank Metrics',
    metrics: {
      irrBeforeTax: 'IRR before Tax',
      irrAfterTax: 'IRR after Tax',
      npvBeforeTax: 'NPV before Tax',
      npvAfterTax: 'NPV after Tax',
      cashOnCash: 'Cash-on-Cash',
      equityMultiple: 'Equity Multiple',
      roi: 'Return on Equity p.a.',
      dscrMin: 'DSCR (Minimum)',
      dscrAvg: 'DSCR (Average)',
      icrMin: 'ICR (Minimum)',
      ltvInitial: 'LTV initial',
      ltvFinal: 'LTV final',
      breakEvenRent: 'Break-Even Rent'
    },
    cashflow: {
      year: 'Year',
      grossRent: 'Gross Rent',
      vacancy: 'Vacancy',
      effectiveRent: 'Effective Rent',
      operatingCosts: 'Operating Costs',
      noi: 'NOI',
      debtService: 'Debt Service',
      interest: 'Interest',
      principal: 'Principal',
      capex: 'CapEx',
      beforeTax: 'CF before Tax',
      tax: 'Tax',
      afterTax: 'CF after Tax',
      cumulative: 'Cumulative',
      debt: 'Outstanding Debt',
      ltv: 'LTV',
      dscr: 'DSCR'
    },
    tax: {
      depreciationRate: 'Depreciation Rate',
      depreciationBasis: 'Depreciation Basis',
      annualDepreciation: 'Annual Depreciation',
      totalDepreciation: 'Total Depreciation',
      totalInterest: 'Total Interest Deduction',
      totalMaintenance: 'Total Maintenance Deduction',
      totalOperating: 'Total Operating Costs',
      totalTax: 'Total Tax Paid',
      totalSavings: 'Tax Savings from Deductions',
      effectiveRate: 'Effective Tax Rate',
      rule15: '15% Rule',
      rule15Triggered: '15% Rule Triggered',
      rule15NotTriggered: '15% Rule Not Triggered',
      rule15Amount: 'Maintenance in 3 Years',
      bridge: {
        income: 'Rental Income',
        depreciation: 'Depreciation',
        interest: 'Interest',
        maintenance: 'Maintenance',
        operating: 'Operating Costs',
        taxableIncome: 'Taxable Income',
        taxPayment: 'Tax Payment'
      }
    },
    risk: {
      maintenance: 'Maintenance Risk',
      liquidity: 'Liquidity Risk',
      low: 'Low',
      medium: 'Medium',
      high: 'High',
      critical: 'Critical'
    },
    totals: {
      totalEquity: 'Total Equity Invested',
      totalCashflowBefore: 'Total Cashflow before Tax',
      totalCashflowAfter: 'Total Cashflow after Tax'
    }
  },

  results: {
    title: 'Results',
    cashflow: 'Cash Flow',
    liquidity: 'Liquidity',
    taxBridge: 'Tax Bridge',
    charts: {
      monthly: 'Monthly',
      yearly: 'Yearly',
      cumulative: 'Cumulative'
    }
  },

  help: {
    title: 'Help',
    glossary: 'Glossary',
    faq: 'FAQ',
    contact: 'Contact'
  },

  admin: {
    title: 'Administration',
    users: 'Users',
    userCount: 'Registered Users',
    projectCount: 'Total Projects',
    userDetail: 'User Details',
    userInfo: 'User Information',
    roles: 'Roles',
    registeredAt: 'Registered',
    projects: 'Projects',
    noProjects: 'No projects',
    backToUsers: 'Back to overview'
  },

  errors: {
    required: 'This field is required',
    invalidEmail: 'Invalid email address',
    minLength: 'Minimum {min} characters required',
    maxLength: 'Maximum {max} characters allowed',
    minValue: 'Minimum value: {min}',
    maxValue: 'Maximum value: {max}',
    positiveNumber: 'Must be a positive number',
    invalidDate: 'Invalid date',
    networkError: 'Network error. Please check your connection.',
    serverError: 'Server error. Please try again later.',
    unknownError: 'An unknown error occurred.'
  }
};
