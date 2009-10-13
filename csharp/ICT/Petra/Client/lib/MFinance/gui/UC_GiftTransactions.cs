/* auto generated with nant generateWinforms from UC_GiftTransactions.yaml and template controlMaintainTable
 *
 * DO NOT edit manually, DO NOT edit with the designer
 *
 */
/*************************************************************************
 *
 * DO NOT REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
 *
 * @Authors:
 *       auto generated
 *
 * Copyright 2004-2009 by OM International
 *
 * This file is part of OpenPetra.org.
 *
 * OpenPetra.org is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * OpenPetra.org is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with OpenPetra.org.  If not, see <http://www.gnu.org/licenses/>.
 *
 ************************************************************************/
using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using Ict.Petra.Shared;
using System.Resources;
using System.Collections.Specialized;
using Mono.Unix;
using Ict.Common;
using Ict.Common.Verification;
using Ict.Petra.Client.App.Core;
using Ict.Petra.Client.App.Core.RemoteObjects;
using Ict.Common.Controls;
using Ict.Petra.Client.CommonForms;
using Ict.Petra.Shared.MFinance.Gift.Data;

namespace Ict.Petra.Client.MFinance.Gui.Gift
{

  /// auto generated user control
  public partial class TUC_GiftTransactions: System.Windows.Forms.UserControl, Ict.Petra.Client.CommonForms.IFrmPetra
  {
    private TFrmPetraEditUtils FPetraUtilsObject;

    private Ict.Petra.Shared.MFinance.Gift.Data.GiftBatchTDS FMainDS;

    /// constructor
    public TUC_GiftTransactions() : base()
    {
      //
      // Required for Windows Form Designer support
      //
      InitializeComponent();
      #region CATALOGI18N

      // this code has been inserted by GenerateI18N, all changes in this region will be overwritten by GenerateI18N
      this.lblLedgerNumber.Text = Catalog.GetString("Ledger:");
      this.lblBatchNumber.Text = Catalog.GetString("Gift Batch:");
      this.btnNew.Text = Catalog.GetString("&Add");
      this.btnRemove.Text = Catalog.GetString("Remove");
      this.txtDetailDonorKey.ButtonText = Catalog.GetString("Find");
      this.lblDetailDonorKey.Text = Catalog.GetString("Donor:");
      this.lblDetailGiftTransactionAmount.Text = Catalog.GetString("Amount:");
      this.lblDetailMotivationGroup.Text = Catalog.GetString("Motivation Group:");
      this.lblDetailMotivationDetail.Text = Catalog.GetString("Motivation Detail:");
      this.txtDetailRecipientKey.ButtonText = Catalog.GetString("Find");
      this.lblDetailRecipientKey.Text = Catalog.GetString("Recipient:");
      #endregion

    }

    /// helper object for the whole screen
    public TFrmPetraEditUtils PetraUtilsObject
    {
        set
        {
            FPetraUtilsObject = value;
        }
    }

    /// dataset for the whole screen
    public Ict.Petra.Shared.MFinance.Gift.Data.GiftBatchTDS MainDS
    {
        set
        {
            FMainDS = value;
        }
    }

    /// needs to be called after FMainDS and FPetraUtilsObject have been set
    public void InitUserControl()
    {
      FPetraUtilsObject.SetStatusBarText(txtDetailGiftTransactionAmount, Catalog.GetString("Enter Your Currency Amount"));
      FPetraUtilsObject.SetStatusBarText(txtDetailRecipientKey, Catalog.GetString("Enter the partner key"));
      grdDetails.Columns.Clear();
      grdDetails.AddTextColumn("Gift Transaction Number", FMainDS.AGiftDetail.ColumnGiftTransactionNumber);
      grdDetails.AddTextColumn("Gift Number", FMainDS.AGiftDetail.ColumnDetailNumber);
      grdDetails.AddTextColumn("Donor Key", FMainDS.AGiftDetail.ColumnDonorKey);
      grdDetails.AddTextColumn("Donor Name", FMainDS.AGiftDetail.ColumnDonorName);
      grdDetails.AddTextColumn("Gift Amount", FMainDS.AGiftDetail.ColumnGiftAmount);
      grdDetails.AddTextColumn("Recipient", FMainDS.AGiftDetail.ColumnRecipientDescription);
      grdDetails.AddTextColumn("Motivation Group", FMainDS.AGiftDetail.ColumnMotivationGroupCode);
      grdDetails.AddTextColumn("Motivation Detail", FMainDS.AGiftDetail.ColumnMotivationDetailCode);
      FPetraUtilsObject.ActionEnablingEvent += ActionEnabledEvent;

      DataView myDataView = FMainDS.AGiftDetail.DefaultView;
      myDataView.AllowNew = false;
      grdDetails.DataSource = new DevAge.ComponentModel.BoundDataView(myDataView);
      grdDetails.AutoSizeCells();

      ShowData();
    }

    /// automatically generated, create a new record of AGiftDetail and display on the edit screen
    public bool CreateNewAGiftDetail()
    {
        // we create the table locally, no dataset
        AGiftDetailRow NewRow = FMainDS.AGiftDetail.NewRowTyped(true);
        NewRowManual(ref NewRow);
        FMainDS.AGiftDetail.Rows.Add(NewRow);

        FPetraUtilsObject.SetChangedFlag();

        grdDetails.DataSource = new DevAge.ComponentModel.BoundDataView(FMainDS.AGiftDetail.DefaultView);
        grdDetails.Refresh();
        SelectDetailRowByDataTableIndex(FMainDS.AGiftDetail.Rows.Count - 1);

        return true;
    }

    private void SelectDetailRowByDataTableIndex(Int32 ARowNumberInTable)
    {
        Int32 RowNumberGrid = -1;
        for (int Counter = 0; Counter < grdDetails.DataSource.Count; Counter++)
        {
            bool found = true;
            foreach (DataColumn myColumn in FMainDS.AGiftDetail.PrimaryKey)
            {
                string value1 = FMainDS.AGiftDetail.Rows[ARowNumberInTable][myColumn].ToString();
                string value2 = (grdDetails.DataSource as DevAge.ComponentModel.BoundDataView).mDataView[Counter][myColumn.Ordinal].ToString();
                if (value1 != value2)
                {
                    found = false;
                }
            }
            if (found)
            {
                RowNumberGrid = Counter + 1;
            }
        }
        grdDetails.Selection.ResetSelection(false);
        grdDetails.Selection.SelectRow(RowNumberGrid, true);
        // scroll to the row
        grdDetails.ShowCell(new SourceGrid.Position(RowNumberGrid, 0), true);

        FocusedRowChanged(this, new SourceGrid.RowEventArgs(RowNumberGrid));
    }

    /// return the selected row
    private AGiftDetailRow GetSelectedDetailRow()
    {
        DataRowView[] SelectedGridRow = grdDetails.SelectedDataRowsAsDataRowView;

        if (SelectedGridRow.Length >= 1)
        {
            return (AGiftDetailRow)SelectedGridRow[0].Row;
        }

        return null;
    }

    private void ShowData()
    {
        FPetraUtilsObject.DisableDataChangedEvent();
        ShowDataManual();
        pnlDetails.Enabled = false;
        if (FMainDS.AGiftDetail != null)
        {
            DataView myDataView = FMainDS.AGiftDetail.DefaultView;
            myDataView.Sort = "a_gift_transaction_number_i ASC";
            myDataView.RowFilter = "a_batch_number_i = " + FBatchNumber.ToString();
            myDataView.AllowNew = false;
            grdDetails.DataSource = new DevAge.ComponentModel.BoundDataView(myDataView);
            grdDetails.AutoSizeCells();
            if (myDataView.Count > 0)
            {
                grdDetails.Selection.ResetSelection(false);
                grdDetails.Selection.SelectRow(1, true);
                FocusedRowChanged(this, new SourceGrid.RowEventArgs(1));
                pnlDetails.Enabled = true;
            }
        }
        FPetraUtilsObject.EnableDataChangedEvent();
    }

    private void ShowDetails(AGiftDetailRow ARow)
    {
        FPetraUtilsObject.DisableDataChangedEvent();
        if (ARow == null)
        {
            pnlDetails.Enabled = false;
            ShowDetailsManual(ARow);
        }
        else
        {
            pnlDetails.Enabled = true;
            FPreviouslySelectedDetailRow = ARow;
            txtDetailGiftTransactionAmount.Text = ARow.GiftTransactionAmount.ToString();
            txtDetailRecipientKey.Text = String.Format("{0:0000000000}", ARow.RecipientKey);
            ShowDetailsManual(ARow);
        }
        FPetraUtilsObject.EnableDataChangedEvent();
    }

    private AGiftDetailRow FPreviouslySelectedDetailRow = null;
    private void FocusedRowChanged(System.Object sender, SourceGrid.RowEventArgs e)
    {
        // get the details from the previously selected row
        if (FPreviouslySelectedDetailRow != null)
        {
            GetDetailsFromControls(FPreviouslySelectedDetailRow);
        }
        // display the details of the currently selected row
        FPreviouslySelectedDetailRow = GetSelectedDetailRow();
        ShowDetails(FPreviouslySelectedDetailRow);
    }

    /// get the data from the controls and store in the currently selected detail row
    public void GetDataFromControls()
    {
        GetDetailsFromControls(FPreviouslySelectedDetailRow);
    }

    private void GetDetailsFromControls(AGiftDetailRow ARow)
    {
        if (ARow != null)
        {
            ARow.BeginEdit();
            ARow.GiftTransactionAmount = Convert.ToDouble(txtDetailGiftTransactionAmount.Text);
            ARow.RecipientKey = Convert.ToInt32(txtDetailRecipientKey.Text);
            GetDetailDataFromControlsManual(ARow);
            ARow.EndEdit();
        }
    }

#region Implement interface functions
    /// auto generated
    public void RunOnceOnActivation()
    {
    }

    /// <summary>
    /// Adds event handlers for the appropiate onChange event to call a central procedure
    /// </summary>
    public void HookupAllControls()
    {
    }

    /// auto generated
    public void HookupAllInContainer(Control container)
    {
        FPetraUtilsObject.HookupAllInContainer(container);
    }

    /// auto generated
    public bool CanClose()
    {
        return FPetraUtilsObject.CanClose();
    }

    /// auto generated
    public TFrmPetraUtils GetPetraUtilsObject()
    {
        return (TFrmPetraUtils)FPetraUtilsObject;
    }
#endregion

#region Action Handling

    /// auto generated
    public void ActionEnabledEvent(object sender, ActionEventArgs e)
    {
        if (e.ActionName == "actNew")
        {
            btnNew.Enabled = e.Enabled;
        }
    }

#endregion
  }
}
