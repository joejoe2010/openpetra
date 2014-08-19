﻿// DO NOT REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
//
// @Authors:
//       Tim Ingham
//
// Copyright 2004-2014 by OM International
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
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Reflection;
using System.Windows.Forms;
using System.Resources;
using System.Collections.Specialized;

using Ict.Common;
using Ict.Common.Verification;
using Ict.Common.Controls;
using Ict.Petra.Client.App.Core;
using Ict.Petra.Client.App.Core.RemoteObjects;
using Ict.Petra.Client.App.Gui;
using Ict.Petra.Client.CommonControls;
using Ict.Petra.Client.CommonForms;
using Ict.Petra.Shared;
using GNU.Gettext;
using Ict.Petra.Shared.MFinance.GL.Data;

namespace Ict.Petra.Client.MFinance.Gui.Setup
{
    public partial class TUC_AccountsList
    {
        private TFrmGLAccountHierarchy FParentForm = null;

        private bool FIsFilterPanelInitialised = false;
        private TextBox FFilterTxtAccountCode = null;
        private TCmbAutoComplete FFilterCmbAccountType = null;
        private TextBox FFilterTxtDescrEnglish = null;
        private TextBox FFilterTxtDescrLocal = null;
        private CheckBox FFilterChkBankAccount = null;
        private CheckBox FFilterChkActive = null;
        private CheckBox FFilterChkSummary = null;
        private CheckBox FFilterChkForeign = null;

        private TSgrdDataGridPaged grdDetails = null;
        private int FPrevRowChangedRow = -1;
        private DataRow FPreviouslySelectedDetailRow = null;

        // The account selected in the parent form
        AccountNodeDetails FSelectedAccount;
//        Int32 FLedgerNumber;
//        String FSelectedHierarchy;
        DataView FDataView = null;

        /// <summary>
        /// I don't want this, but the auto-generated code references it:
        /// </summary>
        public GLSetupTDS MainDS;

        /// <summary>
        /// The Account may have been selected in the tree view, and copied here.
        /// </summary>
        public AccountNodeDetails SelectedAccount
        {
            set
            {
                FSelectedAccount = value;

                if (FDataView != null)
                {
                    Int32 RowIdx = -1;

                    if (FSelectedAccount != null)
                    {
                        RowIdx = FDataView.Find(FSelectedAccount.AccountRow.AccountCode) + 1;
                    }

                    FParentForm.FIAmUpdating++;
                    grdAccounts.SelectRowInGrid(RowIdx);
                    FParentForm.FIAmUpdating--;
                }
            }
        }

        private void InitializeManualCode()
        {
            // The auto-generated code requires that the grid be named grdDetails (for filter/find), but that doesn't work for another part of the autogenerated code!
            // So we make grdDetails reference grdSuppliers here at initialization
            grdDetails = grdAccounts;
        }

        /// <summary>
        /// Perform initialisation
        /// (Actually called earlier than the parent RunOnceOnActivationManual)
        /// </summary>
        public void RunOnceOnActivationManual(TFrmGLAccountHierarchy ParentForm)
        {
            FParentForm = ParentForm;
            grdAccounts.Selection.SelectionChanged += Selection_SelectionChanged;
        }

        void Selection_SelectionChanged(object sender, SourceGrid.RangeRegionChangedEventArgs e)
        {
            if (FParentForm.FIAmUpdating == 0)
            {
                int previousRowId = FPrevRowChangedRow;
                int newRowId = grdAccounts.Selection.ActivePosition.Row;
                DataRowView rowView = (DataRowView)grdAccounts.Rows.IndexToDataSourceRow(newRowId);

                if (rowView == null)
                {
                    FPreviouslySelectedDetailRow = null;
                    FParentForm.SetSelectedAccount(null);
                    FParentForm.PopulateControlsAfterRowSelection();
                    Console.WriteLine("Selected row is NULL");
                }
                else
                {
                    FPreviouslySelectedDetailRow = rowView.Row;
                    String SelectedAccountCode = ((GLSetupTDSAAccountRow)rowView.Row).AccountCode;
                    FParentForm.SetSelectedAccountCode(SelectedAccountCode);

                    if (previousRowId == -1)
                    {
                        FParentForm.PopulateControlsAfterRowSelection();
                    }

                    Console.WriteLine("Row is {0}", FPreviouslySelectedDetailRow.ItemArray[1]);
                }

                FPrevRowChangedRow = newRowId;
            }
            else
            {
                Console.WriteLine("Skipping selection_changed...");
            }
        }

        /// <summary>
        /// Show all the data (Account Code and description)
        /// </summary>
        public void PopulateListView(GLSetupTDS MainDS, Int32 LedgerNumber, String SelectedHierarchy)
        {
//            FLedgerNumber = LedgerNumber;
//            FSelectedHierarchy = SelectedHierarchy;

            FDataView = new DataView(MainDS.AAccount);
            FDataView.Sort = "a_account_code_c";
            FDataView.AllowNew = false;
            grdAccounts.DataSource = new DevAge.ComponentModel.BoundDataView(FDataView);
            grdAccounts.Columns.Clear();
            grdAccounts.AddTextColumn("Code", MainDS.AAccount.ColumnAccountCode);
            grdAccounts.AddTextColumn("Descr", MainDS.AAccount.ColumnAccountCodeShortDesc);
        }

        /// <summary>
        /// Method to collapse the filter panel if it is open
        /// </summary>
        public void CollapseFilterFind()
        {
            if (pnlFilterAndFind.Width > 0)
            {
                // Get the current row
                DataRow currentRow = FPreviouslySelectedDetailRow;

                FFilterAndFindObject.ToggleFilter();
                FParentForm.SetSelectedAccountCode(currentRow.ItemArray[1].ToString());
            }
        }

        private void FilterToggledManual(bool AFilterPanelIsCollapsed)
        {
            if (FIsFilterPanelInitialised)
            {
                return;
            }

            if (!AFilterPanelIsCollapsed)
            {
                FFilterTxtAccountCode = (TextBox)FFilterAndFindObject.FilterPanelControls.FindControlByName("txtAccountCode");
                FFilterCmbAccountType = (TCmbAutoComplete)FFilterAndFindObject.FilterPanelControls.FindControlByName("cmbAccountType");
                FFilterTxtDescrEnglish = (TextBox)FFilterAndFindObject.FilterPanelControls.FindControlByName("txtDescrEnglish");
                FFilterTxtDescrLocal = (TextBox)FFilterAndFindObject.FilterPanelControls.FindControlByName("txtDescrLocal");
                FFilterChkBankAccount = (CheckBox)FFilterAndFindObject.FilterPanelControls.FindControlByName("chkBankAccount");
                FFilterChkActive = (CheckBox)FFilterAndFindObject.FilterPanelControls.FindControlByName("chkActive");
                FFilterChkSummary = (CheckBox)FFilterAndFindObject.FilterPanelControls.FindControlByName("chkSummary");
                FFilterChkForeign = (CheckBox)FFilterAndFindObject.FilterPanelControls.FindControlByName("chkForeign");
                
                FIsFilterPanelInitialised = true;
            }
        }

        private void ApplyFilterManual(ref string AFilterString)
        {
            string filter = String.Empty;

            if (FFilterTxtAccountCode.Text != String.Empty)
            {
                JoinAndAppend(ref filter, String.Format("(a_account_code_c LIKE '%{0}%')", FFilterTxtAccountCode.Text));
            }

            if (FFilterCmbAccountType.Text != String.Empty)
            {
                JoinAndAppend(ref filter, String.Format("(a_account_type_c LIKE '{0}')", FFilterCmbAccountType.Text));
            }

            if (FFilterTxtDescrEnglish.Text != String.Empty)
            {
                JoinAndAppend(ref filter, String.Format("(a_eng_account_code_long_desc_c LIKE '%{0}%')", FFilterTxtDescrEnglish.Text));
            }

            if (FFilterTxtDescrLocal.Text != String.Empty)
            {
                JoinAndAppend(ref filter, String.Format("(a_account_code_long_desc_c LIKE '%{0}%')", FFilterTxtDescrLocal.Text));
            }

            if (FFilterChkBankAccount.CheckState != CheckState.Indeterminate)
            {
                JoinAndAppend(ref filter, String.Format("(a_system_account_flag_l={0})", FFilterChkBankAccount.Checked ? 0 : 1));
            }

            if (FFilterChkActive.CheckState != CheckState.Indeterminate)
            {
                JoinAndAppend(ref filter, String.Format("(a_account_active_flag_l={0})", FFilterChkActive.Checked ? 1 : 0));
            }

            if (FFilterChkSummary.CheckState != CheckState.Indeterminate)
            {
                JoinAndAppend(ref filter, String.Format("(a_posting_status_l={0})", FFilterChkSummary.Checked ? 0 : 1));
            }

            if (FFilterChkForeign.CheckState != CheckState.Indeterminate)
            {
                JoinAndAppend(ref filter, String.Format("(a_posting_status_l={0})", FFilterChkForeign.Checked ? 0 : 1));
            }

            AFilterString = filter;
        }

        private void JoinAndAppend(ref string AStringToExtend, string AStringToAppend)
        {
            if (AStringToExtend.Length > 0)
            {
                AStringToExtend += " AND ";
            }

            AStringToExtend += AStringToAppend;
        }

        private bool IsMatchingRowManual(DataRow ARow)
        {
            return false;
        }

        /// <summary>
        /// Interface method
        /// </summary>
        public void SelectRowInGrid(int ARowToSelect)
        {
            grdDetails.SelectRowInGrid(ARowToSelect, true);
        }
    }
}