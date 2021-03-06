//
// DO NOT REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
//
// @Authors:
//       peters
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
using System.Collections.Generic;
using System.Data;

using Ict.Common;
using Ict.Common.DB;
using Ict.Petra.Shared;
using Ict.Petra.Shared.MPartner;
using Ict.Petra.Shared.MPartner.Partner.Data;
using Ict.Petra.Server.MCommon;
using Ict.Petra.Server.MPartner.Partner.Data.Access;
using Ict.Petra.Server.MSysMan.Maintenance.SystemDefaults.WebConnectors;

namespace Ict.Petra.Server.MFinance.Reporting.WebConnectors
{
    ///<summary>
    /// This WebConnector provides data for the Gift reporting screens
    ///</summary>
    public partial class TFinanceReportingWebConnector
    {
        /// <summary>
        /// Returns a DataTable to the client for use in client-side reporting
        /// </summary>
        [NoRemoting]
        public static DataTable GiftBatchDetailTable(Dictionary <String, TVariant>AParameters, TReportingDbAdapter DbAdapter)
        {
            TDBTransaction Transaction = null;

            int LedgerNumber = AParameters["param_ledger_number_i"].ToInt32();
            int BatchNumber = AParameters["param_batch_number_i"].ToInt32();

            // create new datatable
            DataTable Results = new DataTable();

            DBAccess.GDBAccessObj.BeginAutoReadTransaction(
                ref Transaction,
                delegate
                {
                    DateTime CurrentDate = DateTime.Today;

                    string Query =
                        "SELECT DISTINCT a_gift_batch.a_batch_description_c, a_gift_batch.a_batch_status_c, a_gift_batch.a_gift_type_c, a_gift_batch.a_gl_effective_date_d, "
                        +
                        "a_gift_batch.a_bank_cost_centre_c, a_gift_batch.a_bank_account_code_c, a_gift_batch.a_currency_code_c, a_gift_batch.a_hash_total_n, a_gift_batch.a_batch_total_n, "
                        +

                        "a_gift_detail.a_gift_transaction_number_i, a_gift_detail.a_detail_number_i, a_gift_detail.a_confidential_gift_flag_l, "
                        +
                        "a_gift_detail.p_recipient_key_n, a_gift_detail.a_gift_amount_n, a_gift_detail.a_gift_amount_intl_n, a_gift_detail.a_gift_transaction_amount_n, "
                        +
                        "a_gift_detail.a_motivation_group_code_c, a_gift_detail.a_motivation_detail_code_c, a_gift_detail.a_recipient_ledger_number_n, "
                        +
                        "a_gift_detail.a_gift_comment_one_c, a_gift_detail.a_gift_comment_two_c, a_gift_detail.a_gift_comment_three_c, a_gift_detail.a_tax_deductible_pct_n, "
                        +

                        "a_gift.p_donor_key_n, a_gift.a_reference_c, a_gift.a_method_of_giving_code_c, a_gift.a_method_of_payment_code_c, "
                        +
                        "a_gift.a_receipt_letter_code_c, a_gift.a_date_entered_d, a_gift.a_first_time_gift_l, a_gift.a_receipt_number_i, "
                        +

                        "Donor.p_partner_class_c, Donor.p_partner_short_name_c, Donor.p_receipt_letter_frequency_c, Donor.p_receipt_each_gift_l, "
                        +
                        "Recipient.p_partner_class_c, Recipient.p_partner_short_name_c, " +

                        // true if donor has a valid Ex-Worker special type
                        "CASE WHEN EXISTS (SELECT p_partner_type.* FROM p_partner_type WHERE " +
                        "p_partner_type.p_partner_key_n = a_gift.p_donor_key_n" +
                        " AND (p_partner_type.p_valid_from_d IS null OR p_partner_type.p_valid_from_d <= '" + CurrentDate + "')" +
                        " AND (p_partner_type.p_valid_until_d IS null OR p_partner_type.p_valid_until_d >= '" + CurrentDate + "')" +
                        " AND p_partner_type.p_type_code_c LIKE '" +
                        TSystemDefaults.GetSystemDefault(SharedConstants.SYSDEFAULT_EXWORKERSPECIALTYPE, "EX-WORKER") + "%'" +
                        ") THEN True ELSE False END AS EXWORKER, " +

                        // true if the gift is restricted for the user
                        "CASE WHEN EXISTS (SELECT s_user_group.* FROM s_user_group " +
                        "WHERE a_gift.a_restricted_l IS true" +
                        " AND NOT EXISTS (SELECT s_group_gift.s_read_access_l FROM s_group_gift, s_user_group " +
                        "WHERE s_group_gift.s_read_access_l" +
                        " AND s_group_gift.a_ledger_number_i = " + LedgerNumber +
                        " AND s_group_gift.a_batch_number_i = " + BatchNumber +
                        " AND s_group_gift.a_gift_transaction_number_i = a_gift_detail.a_gift_transaction_number_i" +
                        " AND s_user_group.s_user_id_c = '" + UserInfo.GUserInfo.UserID + "'" +
                        " AND s_user_group.s_group_id_c = s_group_gift.s_group_id_c" +
                        " AND s_user_group.s_unit_key_n = s_group_gift.s_group_unit_key_n)" +
                        ") THEN False ELSE True END AS ReadAccess " +

                        "FROM a_gift_batch, a_gift_detail, a_gift, p_partner AS Donor, p_partner AS Recipient " +

                        "WHERE a_gift_batch.a_ledger_number_i = " + LedgerNumber + " AND a_gift_batch.a_batch_number_i = " + BatchNumber +
                        " AND a_gift.a_ledger_number_i = " + LedgerNumber + " AND a_gift.a_batch_number_i = " + BatchNumber +
                        " AND a_gift_detail.a_ledger_number_i = " + LedgerNumber + " AND a_gift_detail.a_batch_number_i = " +
                        BatchNumber +
                        " AND a_gift.a_gift_transaction_number_i = a_gift_detail.a_gift_transaction_number_i " +
                        " AND Donor.p_partner_key_n = a_gift.p_donor_key_n" +
                        " AND Recipient.p_partner_key_n = a_gift_detail.p_recipient_key_n";

                    Results = DBAccess.GDBAccessObj.SelectDT(Query, "Results", Transaction);
                });

            return Results;
        }

        /// <summary>
        /// Returns a DataTable to the client for use in client-side reporting
        /// </summary>
        [NoRemoting]
        public static DataTable RecipientGiftStatementRecipientTable(Dictionary <String, TVariant>AParameters, TReportingDbAdapter DbAdapter)
        {
            TDBTransaction Transaction = null;

            int LedgerNumber = AParameters["param_ledger_number_i"].ToInt32();
            string RecipientSelection = AParameters["param_recipient"].ToString();
            string ReportType = AParameters["param_report_type"].ToString();
            string OrderBy = AParameters["param_order_by_name"].ToString();
            DateTime CurrentDate = DateTime.Today;

            // create new datatable
            DataTable Results = new DataTable();

            DBAccess.GDBAccessObj.BeginAutoReadTransaction(
                ref Transaction,
                delegate
                {
                    string Query = "SELECT DISTINCT" +
                                   " Recipient.p_partner_key_n AS RecipientKey," +
                                   " Recipient.p_partner_short_name_c AS RecipientName," +
                                   " Recipient.p_partner_class_c AS RecipientClass," +

                                   " CASE WHEN EXISTS (SELECT 1 FROM PUB_p_partner WHERE PUB_p_partner.p_partner_key_n = PUB_p_partner_gift_destination.p_field_key_n"
                                   +
                                   " OR PUB_p_partner.p_partner_key_n = um_unit_structure.um_child_unit_key_n)" +
                                   " THEN PUB_p_partner.p_partner_short_name_c " +
                                   " ELSE 'UNKNOWN'" +
                                   " END AS FieldName" +

                                   " FROM" +
                                   " PUB_a_gift as gift, " +
                                   " PUB_a_gift_detail AS detail," +
                                   " PUB_a_gift_batch," +
                                   " PUB_p_partner AS Recipient";

                    if (RecipientSelection == "Extract")
                    {
                        Query += ", PUB_m_extract," +
                                 " PUB_m_extract_master";
                    }

                    Query += " LEFT JOIN PUB_p_partner_gift_destination" +
                             " ON Recipient.p_partner_class_c = 'FAMILY'" +
                             " AND PUB_p_partner_gift_destination.p_partner_key_n = Recipient.p_partner_key_n" +
                             " AND PUB_p_partner_gift_destination.p_date_effective_d <= '" + CurrentDate + "'" +
                             " AND (PUB_p_partner_gift_destination.p_date_expires_d IS NULL" +
                             " OR (PUB_p_partner_gift_destination.p_date_expires_d >= '" + CurrentDate + "'" +
                             " AND PUB_p_partner_gift_destination.p_date_effective_d <> PUB_p_partner_gift_destination.p_date_expires_d))" +

                             " LEFT JOIN um_unit_structure" +
                             " ON Recipient.p_partner_class_c = 'UNIT'" +
                             " AND um_unit_structure.um_child_unit_key_n = Recipient.p_partner_key_n" +

                             " LEFT JOIN PUB_p_partner" +
                             " ON (PUB_p_partner.p_partner_key_n = PUB_p_partner_gift_destination.p_field_key_n" +
                             " AND EXISTS (SELECT * FROM PUB_p_partner_gift_destination WHERE PUB_p_partner_gift_destination.p_partner_key_n = Recipient.p_partner_key_n))"
                             +
                             " OR (PUB_p_partner.p_partner_key_n = um_unit_structure.um_parent_unit_key_n" +
                             " AND EXISTS (SELECT * FROM um_unit_structure WHERE um_unit_structure.um_child_unit_key_n = Recipient.p_partner_key_n))"
                             +

                             " WHERE";

                    if (RecipientSelection == "Extract")
                    {
                        Query += " detail.p_recipient_key_n =  PUB_m_extract.p_partner_key_n" +
                                 " AND PUB_m_extract.m_extract_id_i = PUB_m_extract_master.m_extract_id_i" +
                                 " AND PUB_m_extract_master.m_extract_name_c = " + AParameters["param_extract_name"].ToString() +
                                 " AND";
                    }

                    Query += " detail.a_ledger_number_i = gift.a_ledger_number_i" +
                             " AND detail.a_batch_number_i = gift.a_batch_number_i" +
                             " AND detail.a_gift_transaction_number_i = gift.a_gift_transaction_number_i" +
                             " AND gift.a_date_entered_d BETWEEN '" + AParameters["param_from_date"].ToDate().ToString("yyyy-MM-dd") +
                             "' AND '" + AParameters["param_to_date"].ToDate().ToString("yyyy-MM-dd") + "'" +
                             " AND gift.a_ledger_number_i = " + LedgerNumber +

                             " AND PUB_a_gift_batch.a_batch_status_c = 'Posted'" +
                             " AND PUB_a_gift_batch.a_batch_number_i = gift.a_batch_number_i" +
                             " AND PUB_a_gift_batch.a_ledger_number_i = " + LedgerNumber +

                             " AND Recipient.p_partner_key_n = detail.p_recipient_key_n";

                    if (ReportType == "List")
                    {
                        Query += " AND detail.p_recipient_key_n  <> 0";
                    }

                    if (RecipientSelection == "One Recipient")
                    {
                        Query += " AND detail.p_recipient_key_n = " + AParameters["param_recipientkey"].ToInt64();
                    }

                    if (OrderBy == "RecipientField")
                    {
                        Query += " ORDER BY FieldName, RecipientKey";
                    }
                    else if (OrderBy == "RecipientKey")
                    {
                        Query += " ORDER BY RecipientKey";
                    }
                    else if (OrderBy == "RecipientName")
                    {
                        Query += " ORDER BY RecipientName";
                    }

                    Results = DbAdapter.RunQuery(Query, "Recipients", Transaction);
                });

            return Results;
        }

        /// <summary>
        /// Returns a DataTable to the client for use in client-side reporting
        /// </summary>
        [NoRemoting]
        public static DataTable RecipientGiftStatementTotalsTable(Dictionary <String, TVariant>AParameters,
            Int64 ARecipientKey,
            TReportingDbAdapter DbAdapter)
        {
            TDBTransaction Transaction = null;

            int LedgerNumber = AParameters["param_ledger_number_i"].ToInt32();
            int CurrentYear = AParameters["param_from_date"].ToDate().Year;
            string Currency = AParameters["param_currency"].ToString().ToUpper() == "BASE" ? "a_gift_amount_n" : "a_gift_amount_intl_n";

            // create new datatable
            DataTable Results = new DataTable();

            DBAccess.GDBAccessObj.BeginAutoReadTransaction(
                ref Transaction,
                delegate
                {
                    string Query = "SELECT " +
                                   " GiftDetail.p_recipient_key_n AS RecipientKey," +

                                   " SUM (" +
                                   " CASE WHEN" +
                                   " Gift.a_date_entered_d >= '" + new DateTime(CurrentYear - 1, 1, 1).ToString("yyyy-MM-dd") + "'" +
                                   " AND Gift.a_date_entered_d <= '" + new DateTime(CurrentYear - 1, 12, 31).ToString("yyyy-MM-dd") + "'" +
                                   " THEN GiftDetail." + Currency +
                                   " ELSE 0" +
                                   " END) AS PreviousYearTotal," +

                                   " SUM (" +
                                   " CASE WHEN" +
                                   " Gift.a_date_entered_d >= '" + new DateTime(CurrentYear, 1, 1).ToString("yyyy-MM-dd") + "'" +
                                   " AND Gift.a_date_entered_d <= '" + AParameters["param_to_date"].ToDate().ToString("yyyy-MM-dd") + "'" +
                                   " THEN GiftDetail." + Currency +
                                   " ELSE 0" +
                                   " END) AS CurrentYearTotal" +

                                   " FROM" +
                                   " PUB_a_gift AS Gift, " +
                                   " PUB_a_gift_detail AS GiftDetail," +
                                   " PUB_a_gift_batch AS GiftBatch" +

                                   " WHERE" +

                                   " GiftDetail.a_ledger_number_i = " + LedgerNumber +
                                   " AND GiftDetail.p_recipient_key_n = " + ARecipientKey +
                                   " AND Gift.a_ledger_number_i = " + LedgerNumber +
                                   " AND Gift.a_batch_number_i = GiftDetail.a_batch_number_i" +
                                   " AND Gift.a_gift_transaction_number_i = GiftDetail.a_gift_transaction_number_i" +
                                   " AND ((Gift.a_date_entered_d >= '" + new DateTime(CurrentYear - 1, 1, 1).ToString("yyyy-MM-dd") + "'" +
                                   " AND Gift.a_date_entered_d <= '" + new DateTime(CurrentYear - 1, 12, 31).ToString("yyyy-MM-dd") + "')" +
                                   " OR (Gift.a_date_entered_d >= '" + new DateTime(CurrentYear, 1, 1).ToString("yyyy-MM-dd") + "'" +
                                   " AND Gift.a_date_entered_d <= '" + AParameters["param_to_date"].ToDate().ToString("yyyy-MM-dd") + "'))" +
                                   " AND GiftBatch.a_ledger_number_i = " + LedgerNumber +
                                   " AND GiftBatch.a_batch_number_i = Gift.a_batch_number_i" +
                                   " AND GiftBatch.a_batch_status_c = 'Posted'" +

                                   " GROUP BY GiftDetail.p_recipient_key_n";

                    Results = DbAdapter.RunQuery(Query, "RecipientTotals", Transaction);
                });

            return Results;
        }

        /// <summary>
        /// Returns a DataTable to the client for use in client-side reporting
        /// </summary>
        [NoRemoting]
        public static DataTable RecipientGiftStatementDonorTable(Dictionary <String, TVariant>AParameters,
            Int64 ARecipientKey,
            TReportingDbAdapter DbAdapter)
        {
            TDBTransaction Transaction = null;

            int LedgerNumber = AParameters["param_ledger_number_i"].ToInt32();
            string ReportType = AParameters["param_report_type"].ToString();
            string Currency = "";

            if ((ReportType == "List") || (ReportType == "Email"))
            {
                Currency = "a_gift_transaction_amount_n";
            }
            else
            {
                Currency = AParameters["param_currency"].ToString().ToUpper() == "BASE" ? "a_gift_amount_n" : "a_gift_amount_intl_n";
            }

            // create new datatable
            DataTable Results = new DataTable();

            DBAccess.GDBAccessObj.BeginAutoReadTransaction(
                ref Transaction,
                delegate
                {
                    string Query = "SELECT" +
                                   " gift.a_date_entered_d AS GiftDate," +
                                   " gift.p_donor_key_n AS DonorKey," +
                                   " DonorPartner.p_partner_short_name_c AS DonorName," +
                                   " DonorPartner.p_partner_class_c AS DonorClass," +
                                   " detail.p_recipient_key_n AS RecipientKey," +
                                   " detail.a_motivation_detail_code_c AS MotivationCode," +
                                   " detail.a_confidential_gift_flag_l AS Confidential," +
                                   " detail." + Currency + " AS GiftAmount," +
                                   " gift.a_receipt_number_i AS Receipt," +
                                   " PUB_a_gift_batch.a_currency_code_c AS GiftCurrency," +
                                   " RecipientLedgerPartner.p_partner_short_name_c AS GiftField," +

                                   " CASE WHEN" +
                                   " (UPPER(detail.a_comment_one_type_c) = 'RECIPIENT' OR UPPER(detail.a_comment_one_type_c) = 'BOTH')" +
                                   " AND '" + ReportType + "' = 'Complete'" +
                                   " THEN detail.a_gift_comment_one_c" +
                                   " ELSE ''" +
                                   " END AS CommentOne," +
                                   " CASE WHEN" +
                                   " UPPER(detail.a_comment_one_type_c) = 'RECIPIENT' OR UPPER(detail.a_comment_one_type_c) = 'BOTH'" +
                                   " AND '" + ReportType + "' = 'Complete'" +
                                   " THEN detail.a_gift_comment_two_c" +
                                   " ELSE ''" +
                                   " END AS CommentTwo," +
                                   " CASE WHEN" +
                                   " UPPER(detail.a_comment_one_type_c) = 'RECIPIENT' OR UPPER(detail.a_comment_one_type_c) = 'BOTH'" +
                                   " AND '" + ReportType + "' = 'Complete'" +
                                   " THEN detail.a_gift_comment_three_c" +
                                   " ELSE ''" +
                                   " END AS CommentThree" +

                                   " FROM" +
                                   " PUB_a_gift as gift," +
                                   " PUB_a_gift_detail as detail," +
                                   " PUB_a_gift_batch," +
                                   " PUB_p_partner AS DonorPartner," +
                                   " PUB_p_partner AS RecipientLedgerPartner" +

                                   " WHERE" +
                                   " detail.a_ledger_number_i = gift.a_ledger_number_i" +
                                   " AND detail.p_recipient_key_n = " + ARecipientKey +
                                   " AND PUB_a_gift_batch.a_batch_status_c = 'Posted'" +
                                   " AND PUB_a_gift_batch.a_batch_number_i = gift.a_batch_number_i" +
                                   " AND PUB_a_gift_batch.a_ledger_number_i = " + LedgerNumber +
                                   " AND gift.a_date_entered_d BETWEEN '" + AParameters["param_from_date"].ToDate().ToString("yyyy-MM-dd") +
                                   "' AND '" + AParameters["param_to_date"].ToDate().ToString("yyyy-MM-dd") + "'" +
                                   " AND DonorPartner.p_partner_key_n = gift.p_donor_key_n" +
                                   " AND RecipientLedgerPartner.p_partner_key_n = detail.a_recipient_ledger_number_n" +
                                   " AND gift.a_ledger_number_i = " + LedgerNumber +
                                   " AND detail.a_batch_number_i = gift.a_batch_number_i" +
                                   " AND detail.a_gift_transaction_number_i = gift.a_gift_transaction_number_i";

                    if ((ReportType == "Complete") || (ReportType == "Gifts Only"))
                    {
                        Query += " ORDER BY gift.a_date_entered_d";
                    }
                    else if (ReportType == "Donors Only")
                    {
                        Query += " ORDER BY DonorPartner.p_partner_short_name_c";
                    }

                    Results = DbAdapter.RunQuery(Query, "Donors", Transaction);
                });

            return Results;
        }

        /// <summary>
        /// Returns a DataTable to the client for use in client-side reporting
        /// </summary>
        [NoRemoting]
        public static DataTable RecipientGiftStatementDonorAddressesTable(Int64 ADonorKey, TReportingDbAdapter DbAdapter)
        {
            TDBTransaction Transaction = null;

            // create new datatable
            DataTable Results = new DataTable();

            Results.Columns.Add("DonorKey", typeof(Int64));

            DBAccess.GDBAccessObj.BeginAutoReadTransaction(
                ref Transaction,
                delegate
                {
                    // get best address for the partner
                    PPartnerLocationTable PartnerLocationDT = PPartnerLocationAccess.LoadViaPPartner(ADonorKey, Transaction);
                    TLocationPK BestAddress = Calculations.DetermineBestAddress(PartnerLocationDT);

                    string QueryLocation = "SELECT" +
                                           " PUB_p_location.p_locality_c AS Locality," +
                                           " PUB_p_location.p_street_name_c," +
                                           " PUB_p_location.p_address_3_c," +
                                           " PUB_p_location.p_postal_code_c," +
                                           " PUB_p_location.p_city_c," +
                                           " PUB_p_location.p_county_c," +
                                           " PUB_p_location.p_country_code_c," +
                                           " PUB_p_country.p_address_order_i" +

                                           " FROM" +
                                           " PUB_p_location" +

                                           " LEFT JOIN PUB_p_country" +
                                           " ON PUB_p_country.p_country_code_c = PUB_p_location.p_country_code_c" +

                                           " WHERE" +
                                           " PUB_p_location.p_site_key_n = " + BestAddress.SiteKey +
                                           " AND PUB_p_location.p_location_key_i = " + BestAddress.LocationKey;

                    Results.Merge(DbAdapter.RunQuery(QueryLocation, "DonorAddresses", Transaction));

                    Results.Rows[0]["DonorKey"] = ADonorKey;
                });

            return Results;
        }

        /// <summary>
        /// Returns a DataTable to the client for use in client-side reporting
        /// </summary>
        [NoRemoting]
        public static DataTable RecipientTaxDeductPctTable(Dictionary <String, TVariant>AParameters, TReportingDbAdapter DbAdapter)
        {
            TDBTransaction Transaction = null;

            // create new datatable
            DataTable Results = new DataTable();

            DBAccess.GDBAccessObj.BeginAutoReadTransaction(ref Transaction,
                delegate
                {
                    DateTime CurrentDate = DateTime.Today;

                    string RecipientSelection = AParameters["param_recipient_selection"].ToString();

                    string Query =
                        "SELECT DISTINCT p_partner_tax_deductible_pct.p_partner_key_n, p_partner_tax_deductible_pct.p_date_valid_from_d, " +
                        "p_partner_tax_deductible_pct.p_percentage_tax_deductible_n, p_partner.p_partner_short_name_c, " +
                        "p_partner_gift_destination.p_field_key_n, um_unit_structure.um_parent_unit_key_n " +

                        "FROM p_partner_tax_deductible_pct " +

                        "LEFT JOIN p_partner " +
                        "ON p_partner.p_partner_key_n = p_partner_tax_deductible_pct.p_partner_key_n " +

                        "LEFT JOIN p_partner_gift_destination " +
                        "ON CASE WHEN p_partner.p_partner_class_c = 'FAMILY' " +
                        "THEN p_partner.p_partner_key_n = p_partner_gift_destination.p_partner_key_n " +
                        "AND p_partner_gift_destination.p_date_effective_d <= '" + CurrentDate + "' " +
                        "AND (p_partner_gift_destination.p_date_expires_d IS NULL " +
                        "OR (p_partner_gift_destination.p_date_expires_d >= '" + CurrentDate + "' " +
                        "AND p_partner_gift_destination.p_date_effective_d <> p_partner_gift_destination.p_date_expires_d)) END " +

                        "LEFT JOIN um_unit_structure " +
                        "ON CASE WHEN p_partner.p_partner_class_c = 'UNIT' " +
                        "THEN NOT EXISTS (SELECT * FROM p_partner_type " +
                        "WHERE p_partner_type.p_partner_key_n = p_partner_tax_deductible_pct.p_partner_key_n " +
                        "AND p_partner_type.p_type_code_c = 'LEDGER') " +
                        "AND um_unit_structure.um_child_unit_key_n = p_partner_tax_deductible_pct.p_partner_key_n " +
                        "AND um_unit_structure.um_child_unit_key_n <> um_unit_structure.um_parent_unit_key_n END";

                    if (RecipientSelection == "one_partner")
                    {
                        Query += " WHERE p_partner_tax_deductible_pct.p_partner_key_n = " + AParameters["param_recipient_key"].ToInt64();
                    }
                    else if (RecipientSelection == "Extract")
                    {
                        // recipient must be part of extract
                        Query += " WHERE EXISTS(SELECT * FROM m_extract, m_extract_master";

                        if (!AParameters["param_chkPrintAllExtract"].ToBool())
                        {
                            Query += ", a_gift_detail, a_gift_batch";
                        }

                        Query += " WHERE p_partner_tax_deductible_pct.p_partner_key_n = m_extract.p_partner_key_n " +
                                 "AND m_extract.m_extract_id_i = m_extract_master.m_extract_id_i " +
                                 "AND m_extract_master.m_extract_name_c = '" + AParameters["param_extract_name"] + "'";

                        if (!AParameters["param_chkPrintAllExtract"].ToBool())
                        {
                            // recipient must have a posted gift
                            Query += " AND a_gift_detail.a_ledger_number_i = " + AParameters["param_ledger_number_i"] +
                                     " AND a_gift_detail.p_recipient_key_n = m_extract.p_partner_key_n " +
                                     "AND a_gift_batch.a_ledger_number_i = " + AParameters["param_ledger_number_i"] +
                                     " AND a_gift_batch.a_batch_number_i = a_gift_detail.a_batch_number_i " +
                                     "AND a_gift_batch.a_batch_status_c = 'Posted'";
                        }

                        Query += ")";
                    }

                    Results = DBAccess.GDBAccessObj.SelectDT(Query, "Results", Transaction);
                });

            return Results;
        }
    }
}