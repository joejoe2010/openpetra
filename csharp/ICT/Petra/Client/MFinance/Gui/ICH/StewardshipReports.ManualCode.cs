﻿//
// DO NOT REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
//
// @Authors:
//       christophert
//       Tim Ingham
//
// Copyright 2004-2012 by OM International
//
// This file is part of OpenPetra.org.
//
// OpenPetra.org is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// OpenPetra.org is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with OpenPetra.org.  If not, see <http://www.gnu.org/licenses/>.
//
using System;
using System.Data;
using System.IO;
using System.Globalization;
using System.Windows.Forms;
using System.Threading;
using GNU.Gettext;
using Ict.Common;
using Ict.Common.Controls;
using Ict.Common.Data;
using Ict.Common.DB;
using Ict.Common.Verification;
using Ict.Common.Remoting.Shared;
using Ict.Common.Remoting.Client;
using Ict.Petra.Client.App.Core;
using Ict.Petra.Client.App.Core.RemoteObjects;
using Ict.Petra.Shared;
using Ict.Petra.Client.MCommon;
using Ict.Petra.Client.CommonControls;
using Ict.Petra.Client.MFinance.Logic;

using Ict.Petra.Shared.MFinance;
using Ict.Petra.Shared.MFinance.Account.Data;
using Ict.Petra.Shared.Interfaces.MFinance;
using Ict.Petra.Client.MReporting.Logic;
using System.Collections;
using System.Collections.Generic;
using Ict.Petra.Client.MReporting.Gui;
using Ict.Petra.Client.MReporting.Gui.MFinance;
using Ict.Petra.Shared.MReporting;

using Ict.Petra.Client.MSysMan.Gui;
using Ict.Common.IO;
using System.Text;


namespace Ict.Petra.Client.MFinance.Gui.ICH
{
    /// manual methods for the generated window
    public partial class TFrmStewardshipReports : System.Windows.Forms.Form
    {
        Int32 FLedgerNumber = 0;
        ALedgerRow FLedgerRow = null;
        FastReportsWrapper MyFastReportsPlugin;
        String FStatusMsg;

        /// <summary>
        /// Write-only Ledger number property
        /// </summary>
        public Int32 LedgerNumber
        {
            set
            {
                FLedgerNumber = value;

                FLedgerRow =
                    ((ALedgerTable)TDataCache.TMFinance.GetCacheableFinanceTable(TCacheableFinanceTablesEnum.LedgerDetails, FLedgerNumber))[0];

                TFinanceControls.InitialiseAvailableFinancialYearsListHOSA(
                    ref cmbYearEnding,
                    FLedgerNumber);

                chkHOSA.CheckedChanged += RefreshReportingOptions;
                chkStewardship.CheckedChanged += RefreshReportingOptions;
                chkFees.CheckedChanged += RefreshReportingOptions;
                //              chkRecipient.CheckedChanged += RefreshReportingOptions;
                cmbReportPeriod.SelectedValueChanged += RefreshReportingOptions;
                RefreshReportingOptions(null, null);
                FPetraUtilsObject.DelegateGenerateReportOverride = GenerateAllSelectedReports;
                FPetraUtilsObject.DelegateViewReportOverride = ViewReportTemplate;
                uco_SelectedFees.LedgerNumber = FLedgerNumber;

                FPetraUtilsObject.LoadDefaultSettings();
            }
        }

        //
        // called on Year change.
        private void RefreshReportPeriodList(object sender, EventArgs e)
        {
            if (cmbYearEnding.SelectedIndex > -1)
            {
                TFinanceControls.InitialiseAvailableFinancialPeriodsList(
                    ref cmbReportPeriod,
                    FLedgerNumber,
                    cmbYearEnding.GetSelectedInt32(),
                    FLedgerRow.CurrentPeriod,
                    false,
                    false);
            }
        }

        //
        // Called on period change.
        private void RefreshICHStewardshipNumberList(object sender, EventArgs e)
        {
            if ((cmbReportPeriod.SelectedIndex > -1) && (cmbYearEnding.SelectedIndex > -1))
            {
                DateTime YearEnding;

                if (DateTime.TryParse(cmbYearEnding.GetSelectedDescription(), out YearEnding))
                {
                    DateTime YearStart = TRemote.MFinance.GL.WebConnectors.DecrementYear(YearEnding).AddDays(1);

                    TFinanceControls.InitialiseICHStewardshipList(ref cmbICHNumber, FLedgerNumber,
                        cmbReportPeriod.GetSelectedInt32(),
                        YearStart.ToShortDateString(),
                        YearEnding.ToShortDateString());
                }
                else
                {
                    TFinanceControls.InitialiseICHStewardshipList(ref cmbICHNumber, FLedgerNumber,
                        cmbReportPeriod.GetSelectedInt32(),
                        null,
                        null);
                }

                cmbICHNumber.SelectedIndex = 0;
            }
        }

        /// <summary>Called from generated code</summary>
        public void ReadControlsManual(TRptCalculator ACalc, TReportActionEnum AReportAction)
        {
        }

        //
        // Called on any report checkbox changed
        private void RefreshReportingOptions(Object Sender, EventArgs e)
        {
            chkStewardship.Enabled = (cmbReportPeriod.SelectedIndex != 0);

            rbtEmailHosa.Enabled =
                rbtReprintHosa.Enabled = chkHOSA.Enabled && chkHOSA.Checked;

            rbtEmailStewardship.Enabled =
                rbtReprintStewardship.Enabled = chkStewardship.Enabled && chkStewardship.Checked;

            /*
             *          rbtEmailFees.Enabled =
             *              rbtReprintFees.Enabled = chkFees.Enabled && chkFees.Checked;
             *          rbtEmailRecipient.Enabled =
             *              rbtReprintRecipient.Enabled = chkRecipient.Enabled && chkRecipient.Checked;
             */
        }

        private void ViewReportTemplate(TRptCalculator ACalc)
        {
            String ReportName = "";

            /*
             *          if (chkRecipient.Enabled && chkRecipient.Checked)
             *          {
             *              ReportName = "Recipient Gift Statement";
             *          }
             */
            if (chkFees.Enabled && chkFees.Checked)
            {
                ReportName = "Fees";
            }

            if (chkStewardship.Enabled && chkStewardship.Checked)
            {
                ReportName = "Stewardship";
            }

            if (chkHOSA.Enabled && chkHOSA.Checked)
            {
                ReportName = "HOSA";
            }

            if (ReportName == "")
            {
                return;
            }

            MyFastReportsPlugin = new FastReportsWrapper(ReportName);

            /*
             *          if (chkRecipient.Enabled && chkRecipient.Checked)
             *          {
             *              MyFastReportsPlugin.SetDataGetter(LoadRecipientReportData);
             *          }
             */
            if (chkFees.Enabled && chkFees.Checked)
            {
                MyFastReportsPlugin.SetDataGetter(LoadFeesReportData);
            }

            if (chkStewardship.Enabled && chkStewardship.Checked)
            {
                MyFastReportsPlugin.SetDataGetter(LoadStewardshipReportData);
            }

            if (chkHOSA.Enabled && chkHOSA.Checked)
            {
                MyFastReportsPlugin.SetDataGetter(LoadHosaReportData);
            }

            MyFastReportsPlugin.DesignReport(ACalc);
        }

        //
        // New methods using the Fast-reports DLL:
        // This form generates a clutch of different reports.

        // In this method, a new FastReportsWrapper is used for each selected report type.

        private void GenerateAllSelectedReports(TRptCalculator ACalc)
        {
            FStatusMsg = "";

            if (chkHOSA.Enabled && chkHOSA.Checked)
            {
                FStatusMsg += Catalog.GetString("\r\nAll HOSAs:");
                MyFastReportsPlugin = new FastReportsWrapper("HOSA");
                MyFastReportsPlugin.SetDataGetter(LoadHosaReportData);
                MyFastReportsPlugin.GenerateReport(ACalc);
            }

            if (chkStewardship.Enabled && chkStewardship.Checked)
            {
                FStatusMsg += Catalog.GetString("\r\nStewardship Report:");
                MyFastReportsPlugin = new FastReportsWrapper("Stewardship");
                MyFastReportsPlugin.SetDataGetter(LoadStewardshipReportData);
                MyFastReportsPlugin.GenerateReport(ACalc);
            }

            if (chkFees.Enabled && chkFees.Checked)
            {
                FStatusMsg += Catalog.GetString("\r\nFees Report:");
                MyFastReportsPlugin = new FastReportsWrapper("Fees");
                MyFastReportsPlugin.SetDataGetter(LoadFeesReportData);
                MyFastReportsPlugin.GenerateReport(ACalc);
            }

            /*
             *          if (chkRecipient.Enabled && chkRecipient.Checked)
             *          {
             *              MyFastReportsPlugin = new FastReportsWrapper("Recipient Gift Statement");
             *              MyFastReportsPlugin.SetDataGetter(LoadRecientReportData);
             *              MyFastReportsPlugin.GenerateReport(ACalc);
             *          }
             */
            FStatusMsg += Catalog.GetString("\r\n\r\nReport generation complete.");
            this.Invoke(new CrossThreadUpdate(ShowReportStatus));
        }

        delegate void CrossThreadUpdate();

        /// <summary>Stupidly complex way of stepping around the Windows non-thread-safe controls problem!</summary>
        private void ShowReportStatus()
        {
            MessageBox.Show(FStatusMsg, Catalog.GetString("Stewardship Reports"));
        }

        private Dictionary <String, TVariant>InitialiseDictionary(TRptCalculator ACalc)
        {
            Shared.MReporting.TParameterList pm = ACalc.GetParameters();
            pm.Add("param_ledger_number_i", FLedgerNumber);
            String LedgerName = TRemote.MFinance.Reporting.WebConnectors.GetLedgerName(FLedgerNumber);
            ACalc.AddStringParameter("param_ledger_name", LedgerName);
            String CurrencyName = (rbtBase.Checked) ? FLedgerRow.BaseCurrency : FLedgerRow.IntlCurrency;
            ACalc.AddParameter("param_currency_name", CurrencyName);

            ACalc.AddStringParameter("param_currency_formatter", "0,0.000");
            ACalc.AddParameter("param_ich_number", pm.Get("param_cmbICHNumber").ToInt32());
            ACalc.AddParameter("param_period", true);

            Int32 period = pm.Get("param_cmbReportPeriod").ToInt32();
            Int32 PeriodStart = Math.Max(1, period);
            Int32 PeriodEnd = period;

            if (PeriodEnd == 0)
            {
                PeriodEnd = TFinanceControls.GetLedgerNumPeriods(FLedgerNumber);
            }

            Int32 Year = pm.Get("param_cmbYearEnding").ToInt32();

            ACalc.AddParameter("param_start_period_i", PeriodStart);
            ACalc.AddParameter("param_end_period_i", PeriodEnd);
            DateTime StartDate = TRemote.MFinance.GL.WebConnectors.GetPeriodStartDate(FLedgerNumber, Year, 0, PeriodStart);
            DateTime EndDate = TRemote.MFinance.GL.WebConnectors.GetPeriodEndDate(FLedgerNumber, Year, 0, PeriodEnd);
            ACalc.AddParameter("param_real_year", StartDate.Year);
            ACalc.AddParameter("param_start_date", StartDate);
            ACalc.AddParameter("param_end_date", EndDate);
            Boolean IsClosed = (Year < FLedgerRow.CurrentFinancialYear) || (PeriodEnd < FLedgerRow.CurrentPeriod);
            ACalc.AddParameter("param_period_closed", IsClosed);
            ACalc.AddParameter("param_year_i", Year);
            ArrayList reportParam = pm.Elems;

            Dictionary <String, TVariant>paramsDictionary = new Dictionary <string, TVariant>();

            foreach (Shared.MReporting.TParameter p in reportParam)
            {
                if (p.name.StartsWith("param") && (p.name != "param_calculation"))
                {
                    paramsDictionary.Add(p.name, p.value);
                }
            }

            return paramsDictionary;
        } // Initialise Dictionary

        private Boolean LoadHosaReportData(TRptCalculator ACalc)
        {
            Dictionary <String, TVariant>paramsDictionary = InitialiseDictionary(ACalc);
            ACalc.AddStringParameter("param_cost_centre_codes", "ALL");
            ACalc.AddStringParameter("param_filter_cost_centres", "");
            ACalc.AddStringParameter("param_linked_partner_cc", ""); // Used for auto-emailing HOSAs, this is usually blank.
            Boolean DataOk = TFrmHOSA.LoadReportDataStaticInner(this, FPetraUtilsObject, MyFastReportsPlugin, ACalc);

            if ((!ACalc.GetParameters().Get("param_design_template").ToBool())
                && (rbtEmailHosa.Checked))
            {
                ACalc.AddStringParameter("param_currency", "Base"); // Always email HOSAs in Base Currency
                FStatusMsg += FastReportsWrapper.AutoEmailReports(FPetraUtilsObject, MyFastReportsPlugin, ACalc, FLedgerNumber, "Foreign");
                return false;
            }

            return DataOk;
        }  // Load Hosa Report Data

        private Boolean LoadStewardshipReportData(TRptCalculator ACalc)
        {
            Dictionary <String, TVariant>paramsDictionary = InitialiseDictionary(ACalc);
            DataTable ReportTable = TRemote.MReporting.WebConnectors.GetReportDataTable("Stewardship", paramsDictionary);

            if (this.IsDisposed)
            {
                return false;
            }

            if (ReportTable == null)
            {
                FPetraUtilsObject.WriteToStatusBar("Report Cancelled.");
                return false;
            }

            MyFastReportsPlugin.RegisterData(ReportTable, "Stewardship");

            Boolean HasData = (ReportTable.Rows.Count > 0);

            if (!HasData)
            {
                FStatusMsg += Catalog.GetString("No Stewardship entries found for selected Run Number.");
            }

            TParameterList Params = ACalc.GetParameters();

            if ((!Params.Get("param_design_template").ToBool())
                && (rbtEmailStewardship.Checked))
            {
                TUC_EmailPreferences.LoadEmailDefaults();
                TSmtpSender EmailSender = new TSmtpSender(
                    TUserDefaults.GetStringDefault("SmtpHost"),
                    TUserDefaults.GetInt16Default("SmtpPort"),
                    TUserDefaults.GetBooleanDefault("SmtpUseSsl"),
                    TUserDefaults.GetStringDefault("SmtpUser"),
                    TUserDefaults.GetStringDefault("SmtpPassword"),
                    "");
                EmailSender.CcEverythingTo = TUserDefaults.GetStringDefault("SmtpCcTo");
                EmailSender.ReplyTo = TUserDefaults.GetStringDefault("SmtpReplyTo");

                if (!EmailSender.FInitOk)
                {
                    FStatusMsg += String.Format(
                        Catalog.GetString(
                            "\r\nFailed to set up the email server.\n    Please check the settings in Preferences / Email.\n    Message returned: \"{0}\""),
                        EmailSender.FErrorStatus
                        );
                    return false;
                }

                String MyCostCentreCode = String.Format("{0:##00}00", FLedgerNumber);
                String PeriodEnd = Params.Get("param_end_date").ToDate().ToString("dd/MM/yyyy");
                Int32 RunNumber = Params.Get("param_cmbICHNumber").ToInt32();
                String CsvAttachment = String.Format("\"{0}\",{1},\"{2}\",{3},\"{4}\",{5}\n", // "OP:1",30/11/2014,\"0200\",09/12/2014,\"USD\",0"

                    "OP:1",                                     // software originator and version ID
                    PeriodEnd,
                    MyCostCentreCode,                           // Field Cost Centre Code
                    DateTime.Now.ToString("dd/MM/yyyy"),
                    FLedgerRow.BaseCurrency,                    // Stewardship Report CSV always in Base Currency
                    RunNumber                                   // Run number
                    );

                foreach (DataRow Row in ReportTable.Rows)
                {
                    CsvAttachment += String.Format("\"{0}\",{1},{2},{3}\n",
                        Row["CostCentreCode"].ToString(),
                        Convert.ToDecimal(Row["Income"]).ToString("0.00", CultureInfo.InvariantCulture),  // Stewardship Report CSV always in Base Currency
                        Convert.ToDecimal(Row["Expense"]).ToString("0.00", CultureInfo.InvariantCulture),
                        Convert.ToDecimal(Row["Xfer"]).ToString("0.00", CultureInfo.InvariantCulture)
                        );
                }

                String EmailBody = TUserDefaults.GetStringDefault("SmtpEmailBody");
                EmailSender.AttachFromStream(new MemoryStream(Encoding.ASCII.GetBytes(CsvAttachment)), "Stewardship_" + MyCostCentreCode + ".csv");
                Boolean SentOk = EmailSender.SendEmail(
                    TUserDefaults.GetStringDefault("SmtpFromAccount"),
                    TUserDefaults.GetStringDefault("SmtpDisplayName"),
                    "tim.ingham@om.org", //ich@om.org
                    "Stewardship Report [" + MyCostCentreCode + "] Period end: " + PeriodEnd + " Run#: " + RunNumber,
                    EmailBody);

                if (SentOk)
                {
                    FStatusMsg += Catalog.GetString("\r\nStewardship report emailed to ICH.");
                }
                else
                {
                    FStatusMsg += Catalog.GetString("\r\nFailed to send Stewardship email to ICH.");
                }

                return false;
            }

            return HasData;
        } // Load Stewardship Report Data

        private Boolean LoadFeesReportData(TRptCalculator ACalc)
        {
            Dictionary <String, TVariant>paramsDictionary = InitialiseDictionary(ACalc);
            DataTable ReportTable = TRemote.MReporting.WebConnectors.GetReportDataTable("Fees", paramsDictionary);

            if (this.IsDisposed)
            {
                return false;
            }

            if (ReportTable == null)
            {
                FPetraUtilsObject.WriteToStatusBar("Report Cancelled.");
                return false;
            }

            String[] SelectedFees = paramsDictionary["param_fee_codes"].ToString().Split(',');
            Int32 FeeCols = SelectedFees.Length;
            paramsDictionary.Add("param_fee_columns", new TVariant(FeeCols));

            DataTable FeeNames = new DataTable();

            for (Int32 Idx = 0; Idx < uco_SelectedFees.MAX_FEE_COUNT; Idx++)
            {
                FeeNames.Columns.Add();
            }

            DataRow Row = FeeNames.NewRow();
            FeeNames.Rows.Add(Row);

            for (Int32 Idx = 0; Idx < FeeCols; Idx++)
            {
                Row[Idx] = SelectedFees[Idx];
            }

            MyFastReportsPlugin.RegisterData(FeeNames, "FeeNames");
            MyFastReportsPlugin.RegisterData(ReportTable, "Fees");
            return true;
        } // Load Fees Report Data

        /*
         *      private Boolean LoadRecipientReportData(TRptCalculator ACalc)
         *      {
         *          Dictionary<String, TVariant> paramsDictionary = InitialiseDictionary(ACalc);
         *          return false;
         *      }  // Load Recipient Report Data
         */
    }
}