//
// DO NOT REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
//
// @Authors:
//       timop,matthiash, peters
//
// Copyright 2004-2010 by OM International
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
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Odbc;
using Ict.Common;
using Ict.Common.Data;
using Ict.Common.DB;
using Ict.Common.Exceptions;
using Ict.Common.Verification;
using Ict.Petra.Server.App.Core.Security;
using Ict.Petra.Server.MFinance.Account.Data.Access;
using Ict.Petra.Server.MFinance.Gift.Data.Access;
using Ict.Petra.Server.MPartner.Partner.Data.Access;
using Ict.Petra.Server.MSysMan.Maintenance.SystemDefaults.WebConnectors;
using Ict.Petra.Shared;
using Ict.Petra.Shared.MFinance;
using Ict.Petra.Shared.MFinance.Account.Data;
using Ict.Petra.Shared.MFinance.Gift.Data;
using Ict.Petra.Shared.MPartner.Partner.Data;

namespace Ict.Petra.Server.MFinance.Gift.WebConnectors
{
    ///<summary>
    /// This connector provides functions to adjust and reverse gifts
    ///</summary>
    public class TAdjustmentWebConnector
    {
        /// <summary>
        /// Get all data that is needed for a reverse or adjust (not Field Adjust)
        /// </summary>
        /// <param name="requestParams">Hashtable containing the given params</param>
        /// <param name="AGiftDS">DataSet containing all gift data needed</param>
        /// <param name="AMessages">Additional messages to display in a messagebox</param>
        [RequireModulePermission("FINANCE-1")]
        public static bool GetGiftsForReverseAdjust(
            Hashtable requestParams, ref GiftBatchTDS AGiftDS, out TVerificationResultCollection AMessages)
        {
            GiftAdjustmentFunctionEnum Function = (GiftAdjustmentFunctionEnum)requestParams["Function"];
            Int32 LedgerNumber = (Int32)requestParams["ALedgerNumber"];
            Int32 GiftDetailNumber = (Int32)requestParams["GiftDetailNumber"];
            Int32 GiftNumber = (Int32)requestParams["GiftNumber"];
            Int32 BatchNumber = (Int32)requestParams["BatchNumber"];

            AMessages = new TVerificationResultCollection();
            GiftBatchTDS MainDS = new GiftBatchTDS();

            TDBTransaction Transaction = null;

            DBAccess.GDBAccessObj.GetNewOrExistingAutoReadTransaction(IsolationLevel.ReadCommitted,
                TEnforceIsolationLevel.eilMinimum,
                ref Transaction,
                delegate
                {
                    // get data needed for new gifts
                    if (Function.Equals(GiftAdjustmentFunctionEnum.ReverseGiftBatch))
                    {
                        AGiftAccess.LoadViaAGiftBatch(MainDS, LedgerNumber, BatchNumber, Transaction);

                        foreach (AGiftRow gift in MainDS.AGift.Rows)
                        {
                            AGiftDetailAccess.LoadViaAGift(MainDS, LedgerNumber, BatchNumber, gift.GiftTransactionNumber, Transaction);
                        }
                    }
                    else
                    {
                        AGiftAccess.LoadByPrimaryKey(MainDS, LedgerNumber, BatchNumber, GiftNumber, Transaction);

                        if (Function.Equals(GiftAdjustmentFunctionEnum.ReverseGiftDetail))
                        {
                            AGiftDetailAccess.LoadByPrimaryKey(MainDS, LedgerNumber, BatchNumber, GiftNumber, GiftDetailNumber, Transaction);
                        }
                        else
                        {
                            AGiftDetailAccess.LoadViaAGift(MainDS, LedgerNumber, BatchNumber, GiftNumber, Transaction);
                        }
                    }
                });

            AGiftDS = MainDS;

            return CheckGiftsNotPreviouslyReversed(AGiftDS, out AMessages);
        }

        /// <summary>
        /// Find all gifts that need their field adjusted
        /// </summary>
        /// <param name="AGiftDS">Gift Batch containing all the data needed for a Field Change Adjustment</param>
        /// <param name="ALedgerNumber"></param>
        /// <param name="ARecipientKey"></param>
        /// <param name="AStartDate">start of period where we want to fix gifts</param>
        /// <param name="AEndDate">end of period where we want to fix gifts</param>
        /// <param name="AOldField">the wrong field</param>
        /// <param name="AMessages">Additional messages to display in a messagebox</param>
        [RequireModulePermission("FINANCE-1")]
        public static bool GetGiftsForFieldChangeAdjustment(ref GiftBatchTDS AGiftDS, Int32 ALedgerNumber,
            Int64 ARecipientKey,
            DateTime AStartDate,
            DateTime AEndDate,
            Int64 AOldField,
            out TVerificationResultCollection AMessages)
        {
            TDBTransaction Transaction = null;
            GiftBatchTDS MainDS = new GiftBatchTDS();

            AMessages = new TVerificationResultCollection();

            DBAccess.GDBAccessObj.GetNewOrExistingAutoReadTransaction(IsolationLevel.ReadCommitted,
                TEnforceIsolationLevel.eilMinimum,
                ref Transaction,
                delegate
                {
                    string SqlStmt = TDataBase.ReadSqlFile("Gift.GetGiftsToAdjustField.sql");

                    List <OdbcParameter>parameters = new List <OdbcParameter>();
                    OdbcParameter param = new OdbcParameter("LedgerNumber", OdbcType.Int);
                    param.Value = ALedgerNumber;
                    parameters.Add(param);
                    param = new OdbcParameter("StartDate", OdbcType.Date);
                    param.Value = AStartDate;
                    parameters.Add(param);
                    param = new OdbcParameter("EndDate", OdbcType.Date);
                    param.Value = AEndDate;
                    parameters.Add(param);
                    param = new OdbcParameter("RecipientKey", OdbcType.BigInt);
                    param.Value = ARecipientKey;
                    parameters.Add(param);
                    param = new OdbcParameter("OldField", OdbcType.BigInt);
                    param.Value = AOldField;
                    parameters.Add(param);

                    DBAccess.GDBAccessObj.Select(MainDS, SqlStmt, MainDS.AGiftDetail.TableName, Transaction, parameters.ToArray());

                    // get additional data
                    foreach (GiftBatchTDSAGiftDetailRow Row in MainDS.AGiftDetail.Rows)
                    {
                        AGiftBatchAccess.LoadByPrimaryKey(MainDS, Row.LedgerNumber, Row.BatchNumber, Transaction);
                        AGiftRow GiftRow =
                            AGiftAccess.LoadByPrimaryKey(MainDS, Row.LedgerNumber, Row.BatchNumber, Row.GiftTransactionNumber, Transaction);

                        Row.DateEntered = GiftRow.DateEntered;
                        Row.DonorKey = GiftRow.DonorKey;
                        Row.DonorName = PPartnerAccess.LoadByPrimaryKey(Row.DonorKey, Transaction)[0].PartnerShortName;
                    }
                });

            AGiftDS = MainDS;

            return CheckGiftsNotPreviouslyReversed(AGiftDS, out AMessages);
        }

        /// <summary>
        /// Check that none of the gifts have been reversed before.
        /// </summary>
        [RequireModulePermission("FINANCE-1")]
        public static bool CheckGiftsNotPreviouslyReversed(GiftBatchTDS AGiftDS, out TVerificationResultCollection AMessages)
        {
            string Message = string.Empty;
            string Message2 = string.Empty;
            int GiftCount = 0;

            AMessages = new TVerificationResultCollection();

            // sort gifts
            AGiftDS.AGiftDetail.DefaultView.Sort = string.Format("{0}, {1}, {2}",
                AGiftDetailTable.GetBatchNumberDBName(),
                AGiftDetailTable.GetGiftTransactionNumberDBName(),
                AGiftDetailTable.GetDetailNumberDBName());

            foreach (DataRowView RowView in AGiftDS.AGiftDetail.DefaultView)
            {
                AGiftDetailRow GiftDetailRow = (AGiftDetailRow)RowView.Row;

                if (GiftDetailRow.ModifiedDetail)
                {
                    Message += "\n" + String.Format(Catalog.GetString("Gift {0} with Detail {1} in Batch {2}"),
                        GiftDetailRow.GiftTransactionNumber, GiftDetailRow.DetailNumber, GiftDetailRow.BatchNumber);

                    GiftCount++;
                }
            }

            if (GiftCount != 0)
            {
                if (GiftCount > 1)
                {
                    Message = String.Format(Catalog.GetString("Cannot reverse or adjust the following gifts:")) + "\n" + Message +
                              "\n\n" + Catalog.GetString("They have already been adjusted or reversed.");
                }
                else if (GiftCount > 0)
                {
                    Message = String.Format(Catalog.GetString("Cannot reverse or adjust the following gift:")) + "\n" + Message +
                              "\n\n" + Catalog.GetString("It has already been adjusted or reversed.");
                }

                AMessages.Add(new TVerificationResult(null, Message, TResultSeverity.Resv_Critical));

                return false;
            }

            return true;
        }

        /// <summary>
        /// Identify the gift detail that needs to be reset as not reversed
        /// </summary>
        /// <param name="ALedgerNumber"></param>
        /// <param name="AReversalIdentification"></param>
        /// <returns></returns>
        [RequireModulePermission("FINANCE-1")]
        public static bool ReversedGiftReset(int ALedgerNumber, string AReversalIdentification)
        {
            int BatchNo;
            int GiftTransNo;
            int DetailNo;

            TVerificationResultCollection Messages = new TVerificationResultCollection();

            int positionFirstNumber = 1;
            int positionSecondBar = AReversalIdentification.IndexOf('|', positionFirstNumber);
            int positionThirdBar = AReversalIdentification.LastIndexOf('|');
            int lenReversalDetails = AReversalIdentification.Length;

            if (!Int32.TryParse(AReversalIdentification.Substring(positionFirstNumber, positionSecondBar - positionFirstNumber), out BatchNo)
                || !Int32.TryParse(AReversalIdentification.Substring(positionSecondBar + 1,
                        positionThirdBar - positionSecondBar - 1), out GiftTransNo)
                || !Int32.TryParse(AReversalIdentification.Substring(positionThirdBar + 1, lenReversalDetails - positionThirdBar - 1), out DetailNo))
            {
                Messages.Add(new TVerificationResult(
                        String.Format(Catalog.GetString("Cannot parse the Modified Detail Key: '{0}'"),
                            AReversalIdentification),
                        String.Format(Catalog.GetString("Unexpected error.")),
                        TResultSeverity.Resv_Critical));

                return false;
            }

            GiftBatchTDS MainDS = new GiftBatchTDS();

            TDBTransaction Transaction = null;
            bool SubmissionOK = false;

            DBAccess.GDBAccessObj.GetNewOrExistingAutoTransaction(IsolationLevel.Serializable,
                ref Transaction,
                ref SubmissionOK,
                delegate
                {
                    try
                    {
                        TLogging.Log(BatchNo.ToString());
                        TLogging.Log(GiftTransNo.ToString());
                        TLogging.Log(DetailNo.ToString());

                        AGiftDetailAccess.LoadByPrimaryKey(MainDS, ALedgerNumber, BatchNo, GiftTransNo, DetailNo, Transaction);

                        TLogging.Log("Count: " + MainDS.AGiftDetail.Count.ToString());

                        AGiftDetailRow giftDetailRow = (AGiftDetailRow)MainDS.AGiftDetail.Rows[0];
                        //Reset gift to not reversed
                        giftDetailRow.ModifiedDetail = false;

                        AGiftDetailAccess.SubmitChanges(MainDS.AGiftDetail, Transaction);

                        MainDS.AGiftBatch.AcceptChanges();

                        SubmissionOK = true;
                    }
                    catch (Exception ex)
                    {
                        TLogging.Log("An Exception occured in ReversedGiftReset:" + Environment.NewLine + ex.ToString());

                        Messages.Add(new TVerificationResult(
                                String.Format(Catalog.GetString("Cannot reset ModifiedDetail for Gift {0} Detail {1} in Batch {2}"),
                                    GiftTransNo, DetailNo, BatchNo),
                                String.Format(Catalog.GetString("Unexpected error.")),
                                TResultSeverity.Resv_Critical));

                        throw ex;
                    }
                });

            return true;
        }

        /// <summary>
        /// Revert or Adjust a Gift, revert a Gift Detail , revert a gift batch
        /// </summary>
        /// <param name="requestParams">Hashtable containing the given params</param>
        /// <param name="AAdjustmentBatchNumber">Batch that adjustment transactions have been added to</param>
        /// <param name="AGiftDS">DataSet containing all gift data needed</param>
        /// <returns>false if error</returns>
        [RequireModulePermission("FINANCE-1")]
        public static bool GiftRevertAdjust(Hashtable requestParams, out int AAdjustmentBatchNumber, GiftBatchTDS AGiftDS)
        {
            AAdjustmentBatchNumber = 0;
            int AdjustmentBatchNo = AAdjustmentBatchNumber;

            if ((AGiftDS == null) || (AGiftDS.AGiftDetail == null) || (AGiftDS.AGiftDetail.Rows.Count == 0))
            {
                TLogging.Log("Empty dataset sent to GiftRevertAdjust");

                return false;
            }

            Int32 ALedgerNumber = (Int32)requestParams["ALedgerNumber"];
            Boolean BatchSelected = (Boolean)requestParams["NewBatchSelected"];
            GiftAdjustmentFunctionEnum Function = (GiftAdjustmentFunctionEnum)requestParams["Function"];
            Int32 GiftDetailNumber = (Int32)requestParams["GiftDetailNumber"];
            bool NoReceipt = (Boolean)requestParams["NoReceipt"];

            DateTime DateEffective;
            decimal batchGiftTotal = 0;
            Int32 ANewBatchNumber = 0;

            if (BatchSelected)
            {
                ANewBatchNumber = (Int32)requestParams["NewBatchNumber"];
            }

            TDBTransaction Transaction = null;
            bool SubmissionOK = false;

            DBAccess.GDBAccessObj.GetNewOrExistingAutoTransaction(IsolationLevel.Serializable,
                ref Transaction,
                ref SubmissionOK,
                delegate
                {
                    try
                    {
                        ALedgerTable LedgerTable = ALedgerAccess.LoadByPrimaryKey(ALedgerNumber, Transaction);

                        AGiftBatchRow giftBatch;

                        // if we need to create a new gift batch
                        if (!BatchSelected)
                        {
                            giftBatch = CreateNewGiftBatch(requestParams, ref AGiftDS, out DateEffective, ref LedgerTable, Transaction);
                        }
                        else  // using an existing gift batch
                        {
                            AGiftBatchAccess.LoadByPrimaryKey(AGiftDS, ALedgerNumber, ANewBatchNumber, Transaction);

                            giftBatch = AGiftDS.AGiftBatch[0];
                            DateEffective = giftBatch.GlEffectiveDate;
                            //If into an existing batch, then retrive the existing batch total
                            batchGiftTotal = giftBatch.BatchTotal;
                        }

                        AdjustmentBatchNo = giftBatch.BatchNumber;

                        //assuming new elements are added after these static borders

                        int cycle = 0;

                        AGiftDS.AGift.DefaultView.Sort = string.Format("{0}, {1}",
                            AGiftTable.GetBatchNumberDBName(),
                            AGiftTable.GetGiftTransactionNumberDBName());

                        AGiftDS.AGiftDetail.DefaultView.Sort = string.Format("{0}, {1}, {2}",
                            AGiftDetailTable.GetBatchNumberDBName(),
                            AGiftDetailTable.GetGiftTransactionNumberDBName(),
                            AGiftDetailTable.GetDetailNumberDBName());

                        // first cycle creates gift reversal; second cycle creates new adjusted gift (if needed)
                        do
                        {
                            foreach (DataRowView giftRow in AGiftDS.AGift.DefaultView)
                            {
                                AGiftRow oldGift = (AGiftRow)giftRow.Row;

                                if (oldGift.RowState != DataRowState.Added)
                                {
                                    AGiftRow gift = AGiftDS.AGift.NewRowTyped(true);
                                    DataUtilities.CopyAllColumnValuesWithoutPK(oldGift, gift);
                                    gift.LedgerNumber = giftBatch.LedgerNumber;
                                    gift.BatchNumber = giftBatch.BatchNumber;
                                    gift.DateEntered = DateEffective;
                                    gift.GiftTransactionNumber = giftBatch.LastGiftNumber + 1;
                                    giftBatch.LastGiftNumber++;
                                    gift.LastDetailNumber = 0;

                                    if (NoReceipt)
                                    {
                                        gift.ReceiptLetterCode = "NO*RECET";
                                    }

                                    AGiftDS.AGift.Rows.Add(gift);

                                    foreach (DataRowView giftDetailRow in AGiftDS.AGiftDetail.DefaultView)
                                    {
                                        AGiftDetailRow oldGiftDetail = (AGiftDetailRow)giftDetailRow.Row;

                                        // if gift detail belongs to gift
                                        if ((oldGiftDetail.GiftTransactionNumber == oldGift.GiftTransactionNumber)
                                            && (oldGiftDetail.BatchNumber == oldGift.BatchNumber)
                                            && (!Function.Equals(GiftAdjustmentFunctionEnum.ReverseGiftDetail)
                                                || (oldGiftDetail.DetailNumber == GiftDetailNumber)))
                                        {
                                            AddDuplicateGiftDetailToGift(ref AGiftDS, ref gift, oldGiftDetail, cycle == 0, null, requestParams);

                                            batchGiftTotal += oldGiftDetail.GiftTransactionAmount * ((cycle == 0) ? -1 : 1);

                                            // original gift also gets marked as a reversal
                                            oldGiftDetail.ModifiedDetail = true;
                                        }
                                    }
                                }
                            }

                            cycle++;
                        } while ((cycle < 2)
                                 && (Function.Equals(GiftAdjustmentFunctionEnum.AdjustGift) || Function.Equals(GiftAdjustmentFunctionEnum.FieldAdjust)
                                     || Function.Equals(GiftAdjustmentFunctionEnum.TaxDeductiblePctAdjust)));

                        //When reversing into a new or existing batch, set batch total
                        if (!Function.Equals(GiftAdjustmentFunctionEnum.AdjustGift))
                        {
                            giftBatch.BatchTotal = batchGiftTotal;
                        }

                        // save everything at the end
                        AGiftBatchAccess.SubmitChanges(AGiftDS.AGiftBatch, Transaction);

                        ALedgerAccess.SubmitChanges(LedgerTable, Transaction);

                        AGiftAccess.SubmitChanges(AGiftDS.AGift, Transaction);

                        AGiftDetailAccess.SubmitChanges(AGiftDS.AGiftDetail, Transaction);

                        AGiftDS.AGiftBatch.AcceptChanges();

                        SubmissionOK = true;
                    }
                    catch (Exception Exc)
                    {
                        TLogging.Log("An Exception occured while performing Gift Reverse/Adjust:" + Environment.NewLine + Exc.ToString());

                        throw new EOPAppException(Catalog.GetString("Gift Reverse/Adjust failed."), Exc);
                    }
                });

            AAdjustmentBatchNumber = AdjustmentBatchNo;

            return SubmissionOK;
        }

        /// create a new gift batch using some of the details of an existing gift batch
        private static AGiftBatchRow CreateNewGiftBatch(Hashtable requestParams,
            ref GiftBatchTDS AMainDS,
            out DateTime ADateEffective,
            ref ALedgerTable ALedgerTable,
            TDBTransaction ATransaction)
        {
            AGiftBatchRow ReturnValue;

            Int32 LedgerNumber = (Int32)requestParams["ALedgerNumber"];

            ADateEffective = (DateTime)requestParams["GlEffectiveDate"];
            GiftAdjustmentFunctionEnum Function = (GiftAdjustmentFunctionEnum)requestParams["Function"];
            Int32 BatchNumber = (Int32)requestParams["BatchNumber"];

            AGiftBatchAccess.LoadByPrimaryKey(AMainDS, LedgerNumber, BatchNumber, ATransaction);

            AGiftBatchRow oldGiftBatch = AMainDS.AGiftBatch[0];
            TGiftBatchFunctions.CreateANewGiftBatchRow(ref AMainDS, ref ATransaction, ref ALedgerTable, LedgerNumber, ADateEffective);
            ReturnValue = AMainDS.AGiftBatch[1];
            ReturnValue.BankAccountCode = oldGiftBatch.BankAccountCode;
            ReturnValue.BankCostCentre = oldGiftBatch.BankCostCentre;
            ReturnValue.CurrencyCode = oldGiftBatch.CurrencyCode;
            ReturnValue.ExchangeRateToBase = oldGiftBatch.ExchangeRateToBase;
            ReturnValue.MethodOfPaymentCode = oldGiftBatch.MethodOfPaymentCode;
            ReturnValue.HashTotal = 0;

            if (ReturnValue.MethodOfPaymentCode.Length == 0)
            {
                ReturnValue.SetMethodOfPaymentCodeNull();
            }

            ReturnValue.BankCostCentre = oldGiftBatch.BankCostCentre;
            ReturnValue.GiftType = oldGiftBatch.GiftType;

            if (Function.Equals(GiftAdjustmentFunctionEnum.AdjustGift))
            {
                ReturnValue.BatchDescription = Catalog.GetString("Gift Adjustment");
            }
            else if (Function.Equals(GiftAdjustmentFunctionEnum.FieldAdjust))
            {
                ReturnValue.BatchDescription = Catalog.GetString("Gift Adjustment (Field Change)");
            }
            else if (Function.Equals(GiftAdjustmentFunctionEnum.TaxDeductiblePctAdjust))
            {
                ReturnValue.BatchDescription = Catalog.GetString("Gift Adjustment (Tax Deductible Pct Change)");
            }
            else
            {
                ReturnValue.BatchDescription = Catalog.GetString("Reverse Gift");
            }

            return ReturnValue;
        }

        /// <summary>
        /// Adds a duplicate Gift Detail (or reversed duplicate GiftDetail) to Gift.
        /// </summary>
        /// <param name="AMainDS"></param>
        /// <param name="AGift"></param>
        /// <param name="AOldGiftDetail"></param>
        /// <param name="AReversal">True for reverse or false for straight duplicate</param>
        /// <param name="AGiftCommentOne"></param>
        /// <param name="ARequestParams"></param>
        private static void AddDuplicateGiftDetailToGift(ref GiftBatchTDS AMainDS,
            ref AGiftRow AGift,
            AGiftDetailRow AOldGiftDetail,
            bool AReversal,
            string AGiftCommentOne,
            Hashtable ARequestParams = null)
        {
            bool TaxDeductiblePercentageEnabled = Convert.ToBoolean(
                TSystemDefaults.GetSystemDefault(SharedConstants.SYSDEFAULT_TAXDEDUCTIBLEPERCENTAGE, "FALSE"));

            GiftAdjustmentFunctionEnum Function = (GiftAdjustmentFunctionEnum)ARequestParams["Function"];

            AGiftDetailRow giftDetail = AMainDS.AGiftDetail.NewRowTyped(true);

            DataUtilities.CopyAllColumnValuesWithoutPK(AOldGiftDetail, giftDetail);

            giftDetail.DetailNumber = ++AGift.LastDetailNumber;
            AGift.LastDetailNumber++;

            giftDetail.LedgerNumber = AGift.LedgerNumber;
            giftDetail.BatchNumber = AGift.BatchNumber;
            giftDetail.GiftTransactionNumber = AGift.GiftTransactionNumber;

            decimal signum = (AReversal) ? -1 : 1;
            giftDetail.GiftTransactionAmount = signum * AOldGiftDetail.GiftTransactionAmount;
            giftDetail.GiftAmount = signum * AOldGiftDetail.GiftAmount;
            giftDetail.GiftAmountIntl = signum * AOldGiftDetail.GiftAmountIntl;

            if (TaxDeductiblePercentageEnabled)
            {
                if (Function.Equals(GiftAdjustmentFunctionEnum.TaxDeductiblePctAdjust) && !AReversal)
                {
                    giftDetail.TaxDeductiblePct = Convert.ToDecimal(ARequestParams["NewPct"]);
                    TaxDeductibility.UpdateTaxDeductibiltyAmounts(ref giftDetail);
                }
                else
                {
                    giftDetail.TaxDeductibleAmount = signum * AOldGiftDetail.TaxDeductibleAmount;
                    giftDetail.TaxDeductibleAmountBase = signum * AOldGiftDetail.TaxDeductibleAmountBase;
                    giftDetail.TaxDeductibleAmountIntl = signum * AOldGiftDetail.TaxDeductibleAmountIntl;
                    giftDetail.NonDeductibleAmount = signum * AOldGiftDetail.NonDeductibleAmount;
                    giftDetail.NonDeductibleAmountBase = signum * AOldGiftDetail.NonDeductibleAmountBase;
                    giftDetail.NonDeductibleAmountIntl = signum * AOldGiftDetail.NonDeductibleAmountIntl;
                }
            }

            if (AGiftCommentOne != null)
            {
                giftDetail.GiftCommentOne = AGiftCommentOne;
            }

            if (ARequestParams != null)
            {
                giftDetail.GiftCommentOne = (String)ARequestParams["ReversalCommentOne"];
                giftDetail.GiftCommentTwo = (String)ARequestParams["ReversalCommentTwo"];
                giftDetail.GiftCommentThree = (String)ARequestParams["ReversalCommentThree"];
                giftDetail.CommentOneType = (String)ARequestParams["ReversalCommentOneType"];
                giftDetail.CommentTwoType = (String)ARequestParams["ReversalCommentTwoType"];
                giftDetail.CommentThreeType = (String)ARequestParams["ReversalCommentThreeType"];
            }

            // If reversal: mark the new gift as a reversal
            if (AReversal)
            {
                giftDetail.ModifiedDetail = true;

                //Identify the reversal source
                giftDetail.ModifiedDetailKey = "|" + AOldGiftDetail.BatchNumber.ToString() + "|" +
                                               AOldGiftDetail.GiftTransactionNumber.ToString() + "|" +
                                               AOldGiftDetail.DetailNumber.ToString();
            }
            else
            {
                giftDetail.ModifiedDetail = false;
            }

            AMainDS.AGiftDetail.Rows.Add(giftDetail);
        }
    }
}