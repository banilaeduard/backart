using ClosedXML.Excel;
using EntityDto;

namespace WorkSheetServices
{
    public static class WorkbookReportsService
    {
        public static byte[] GenerateReport(List<DispozitieLivrare> dispozitii,
            Func<DispozitieLivrare, string> keyResolver,
            Func<DispozitieLivrare, string> grouping1,
            Func<DispozitieLivrare, string> grouping2,
            string pageOrientation = "landscape")
        {
            using (var ms = new MemoryStream())
            {
                using (var workbook = new XLWorkbook())
                {
                    var results = dispozitii.GroupBy(keyResolver).OrderByDescending(x => x.DistinctBy(grouping2).Count());
                    var ids = results.Select(x => x.Key).Distinct().ToArray();

                    var firstEmptyCol = 3;
                    var lastColCountIndex = firstEmptyCol + ids.Count() - 1;

                    var worksheet = workbook.AddWorksheet("Rezultate unite");
                    worksheet.Style.Font.FontSize = 15;
                    var lines = results.ToList();

                    var lastCol = lastColCountIndex < 22 ? ((char)(lastColCountIndex + 64)).ToString() : "Z";
                    worksheet.Range(@$"A1:{lastCol}1").Merge();
                    worksheet.Cell(1, 1).Value = "Dispozitii Livrare: " + string.Join("; ", lines.Select(t =>
                        string.Format("{0} ( {1} )", t.Key, string.Join(", ", t.Select(t => t.NumarIntern).Distinct()))
                    ).ToArray());

                    int firstRow = 2;

                    worksheet.Row(firstRow).Cells("1:2").Style.Border.SetBottomBorder(XLBorderStyleValues.Medium);
                    worksheet.Cell(firstRow, 1).Value = "Nume Produs";
                    worksheet.Cell(firstRow, 2).Value = "Q";

                    var extraInfo = results.ToDictionary(t => t.Key);
                    for (var z = 0; z < ids.Count(); z++)
                    {
                        worksheet.Cell(firstRow, firstEmptyCol + z).Value = ids[z];
                        worksheet.Cell(firstRow, firstEmptyCol + z).WorksheetColumn().Width = 10;
                        worksheet.Cell(firstRow, firstEmptyCol + z).Style.Alignment.ShrinkToFit = true;

                        if (z % 2 == 0)
                            worksheet.Cell(firstRow, firstEmptyCol + z).Style.Fill.SetBackgroundColor(XLColor.LightGray);
                    }

                    workbook.FullCalculationOnLoad = true;
                    int i = firstRow + 1;
                    XLColor[] skipcolors = [XLColor.Gainsboro, XLColor.Bisque, XLColor.LightGray, XLColor.MistyRose];
                    var cnt = skipcolors.Length;

                    var values = dispozitii.GroupBy(grouping1).OrderByDescending(t => t.Key).ToList();
                    foreach (var grp in values)
                    {
                        int grp_i = i;
                        var ordered = grp.GroupBy(grouping2).OrderBy(t => t.Key).ToList();

                        var currentColorint = 0;
                        int currentColoredRow = 1;

                        foreach (var x in ordered)
                        {
                            var colorPrint = skipcolors[currentColorint % cnt];
                            if (currentColoredRow % 2 == 0)
                            {
                                worksheet.Row(i).Cells(string.Format("1:{0}", firstEmptyCol - 1)).Style.Fill.SetBackgroundColor(colorPrint);
                            }
                            worksheet.Cell(i, 1).Value = x.Key;
                            worksheet.Cell(i, 2).FormulaR1C1 = string.Format("=SUM(R{0}C3:R{0}C{1})", i, lastColCountIndex);
                            worksheet.Cell(i, 2).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                            for (var z = 0; z < ids.Count(); z++)
                            {
                                var items = x.Where(t => keyResolver(t) == ids[z]).ToList();

                                if (items.Any())
                                {
                                    worksheet.Cell(i, firstEmptyCol + z).Value = items.Sum(t => t.Cantitate);
                                    worksheet.Cell(i, firstEmptyCol + z).Style.Border.SetRightBorder(XLBorderStyleValues.Thin);
                                    worksheet.Cell(i, firstEmptyCol + z).Style.Border.SetRightBorderColor(XLColor.Black);
                                    worksheet.Cell(i, firstEmptyCol + z).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                                    if (currentColoredRow % 2 == 0)
                                    {
                                        worksheet.Cell(i, firstEmptyCol + z).Style.Fill.SetBackgroundColor(colorPrint);
                                    }
                                }
                            }

                            if (currentColoredRow++ % 2 == 0)
                            {
                                currentColorint++;
                            }
                            i++;
                        }

                        if (ordered.Count() > 1)
                        {
                            worksheet.Cell(i, 1).Value = string.Format("Total {0}:", GetDisplayName(grp.Key));
                            worksheet.Cell(i, 1).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
                            worksheet.Cell(i, 1).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);

                            worksheet.Cell(i, 2).FormulaR1C1 = string.Format("=SUM(R{0}C3:R{1}C{2})", grp_i, i - 1, lastColCountIndex);
                            worksheet.Cell(i, 2).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
                            worksheet.Cell(i, 2).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                            worksheet.Cell(i, 2).AddConditionalFormat().WhenGreaterThan(0).Fill.SetBackgroundColor(XLColor.LightCyan);

                            for (var z = 0; z < ids.Count(); z++)
                            {
                                worksheet.Cell(i, firstEmptyCol + z).FormulaR1C1 = string.Format("=SUM(R{0}C{1}:R{2}C{3})", grp_i, firstEmptyCol + z, i - 1, firstEmptyCol + z);
                                worksheet.Cell(i, firstEmptyCol + z).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                                worksheet.Cell(i, firstEmptyCol + z).AddConditionalFormat().WhenGreaterThan(0).Fill.SetBackgroundColor(XLColor.LightCyan);
                            }
                            worksheet.Row(i).Cells($@"{firstEmptyCol}:{firstEmptyCol + ids.Count() - 1}").Style.Border.SetOutsideBorder(XLBorderStyleValues.Dashed);
                            i++;
                        }
                        else
                        {
                            worksheet.Row(i - 1).Cells(@$"1:{lastColCountIndex}").Style.Fill.SetBackgroundColor(XLColor.LightCyan);
                        }
                    }

                    var table = worksheet.Range(firstRow, 1, i - 1, 2).CreateTable();
                    table.Theme = XLTableTheme.None;

                    worksheet.Columns("1:2").AdjustToContents();

                    i++;
                    foreach (var val in lines)
                    {
                        worksheet.Range(@$"A{i}:{lastCol}{i}").Merge();
                        worksheet.Range(@$"A{i + 1}:{lastCol}{i + 1}").Merge();
                        worksheet.Cell(i, 1).Value = val.First().NumeLocatie + ": " + string.Join("; ", val.DistinctBy(t => t.DataDocument).Select(x => x.DataDocument.ToString("dd/MM/yyyy")));
                        worksheet.Cell(i++, 1).Style.Font.SetFontColor(XLColor.RedMunsell);

                        worksheet.Cell(i, 1).Value = string.Join(";", val.DistinctBy(t => t.NumarComanda).Select(x => x.NumarComanda));
                        worksheet.Cell(i++, 1).Style.Alignment.ShrinkToFit = true;
                    }

                    //worksheet.Rows().AdjustToContents();

                    int[] colIndex = [1, 4];
                    int[] colRowIndex = [1, 1];

                    var worksheet2 = workbook.AddWorksheet("per location");
                    worksheet2.Style.Font.FontSize = 14;
                    var colCount = 2;

                    for (int idx = 0; idx < lines.Count; idx++)
                    {
                        var col = colIndex[idx % colIndex.Length];
                        var rowIdx = colRowIndex[idx % colIndex.Length];

                        var firstCol = ((char)(col + 64)).ToString();
                        var lastCol2 = ((char)(col + 64 + colCount - 1)).ToString();

                        var series = lines[idx].GroupBy(grouping1).OrderByDescending(t => t.Key).ToList();

                        worksheet2.Cell(++rowIdx, col).Value = lines[idx].First().NumeLocatie;
                        worksheet2.Cell(rowIdx++, col).Style.Fill.SetBackgroundColor(XLColor.Yellow);

                        foreach (var line in series)
                        {
                            var grouping = line.GroupBy(x => GetSummary(x.CodProdus)).ToList();
                            var initialRow = rowIdx;
                            foreach (var t in grouping)
                            {
                                worksheet2.Cell(rowIdx, col).Value = t.Key;
                                worksheet2.Cell(rowIdx, col + 1).Value = t.Sum(x => x.Cantitate);

                                rowIdx++;
                            }
                            if (grouping.Count() > 1)
                            {
                                worksheet2.Range(@$"{firstCol}{rowIdx}:{lastCol2}{rowIdx}").Style.Border.SetTopBorder(XLBorderStyleValues.Dotted);
                            }
                            else
                            {
                                worksheet2.Range(@$"{firstCol}{rowIdx - 1}:{lastCol2}{rowIdx - 1}").Style.Fill.SetBackgroundColor(XLColor.LightCyan);
                            }
                        }

                        colRowIndex[idx % colIndex.Length] = rowIdx;

                        var locSheet = workbook.AddWorksheet(lines[idx].Key);
                        locSheet.Style.Font.FontSize = 13;

                        locSheet.Range("A1:C1").Merge();
                        locSheet.Cell(1, 1).Value = lines[idx].First().NumeLocatie;
                        locSheet.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        locSheet.Cell(1, 1).Style.Font.FontSize = 15;

                        locSheet.Range("A2:C2").Merge();
                        locSheet.Cell(2, 1).Value = $"Numar Intern: {string.Join("; ", lines[idx].Select(x => x.NumarIntern).Distinct())}";
                        locSheet.Cell(2, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        locSheet.Cell(2, 1).Style.Font.FontSize = 9;

                        locSheet.Cell(3, 1).Value = "Denumire produs";
                        locSheet.Cell(3, 1).Style.Font.Bold = true;

                        locSheet.Cell(3, 2).Value = "Buc";
                        locSheet.Cell(3, 2).Style.Font.Bold = true;

                        locSheet.Cell(3, 3).Value = "Detalii";
                        locSheet.Cell(3, 3).Style.Font.Bold = true;
                        locSheet.Range("A3:C3").Style.Border.SetOutsideBorder(XLBorderStyleValues.Double);

                        var cRow = 4;
                        foreach (var entry in lines[idx].OrderByDescending(x => x.DataDocumentBaza).GroupBy(x => new { c = x.NumarComanda, d = !string.IsNullOrEmpty(x.DetaliiDoc) || !string.IsNullOrEmpty(x.DetaliiLinie) }))
                        {
                            var detaliiDoc = string.Join(" ",
                                entry.First().DetaliiDoc.Replace("\n", " ")
                                .Split(" ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                );
                            string comandaDetails = $"Comanda {entry.Key.c} - {entry.First().DataDocumentBaza?.ToString("dd/MM/yy")}{(string.IsNullOrWhiteSpace(detaliiDoc) ? "" : $" - {detaliiDoc}")}";

                            var rows = SplitRow(locSheet, cRow, 1, comandaDetails, 75);
                            if (rows == 0)
                            {
                                //locSheet.Range($"A{cRow}:{(string.IsNullOrWhiteSpace(detaliiDoc) ? "B" : "C")}{cRow}").Merge();
                            }
                            if (!string.IsNullOrEmpty(detaliiDoc))
                            {
                                locSheet.Cell(cRow, 1).Style.Font.FontSize = 12;
                            }
                            locSheet.Cell(cRow, 1).Style.Fill.SetBackgroundColor(XLColor.LightBlue);
                            cRow += rows + 1;

                            var comList = entry.GroupBy(x => new { x = grouping1(x), p = grouping2(x), l = string.Join(" ", x.DetaliiLinie?.Replace("\n", " ").Split(" ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)) }).OrderByDescending(x => x.Key.x).ToList();

                            var cLine = 0;
                            foreach (var com in comList)
                            {
                                var rowsMereged = SplitRow(locSheet, cRow + cLine, 3, com.Key.l, 25, "C{0}:C{1}");
                                locSheet.Cell(cRow + cLine, 3).WorksheetColumn().Width = 25;
                                locSheet.Cell(cRow + cLine, 3).Style.Font.FontSize = 10;
                                if (rowsMereged > 0)
                                {
                                    locSheet.Range($"A{cRow + cLine}:A{cRow + cLine + rowsMereged}").Merge();
                                    locSheet.Range($"B{cRow + cLine}:B{cRow + cLine + rowsMereged}").Merge();
                                    locSheet.Range($"A{cRow + cLine}:C{cRow + cLine + rowsMereged}").Style.Border.SetOutsideBorder(XLBorderStyleValues.Dashed);
                                }
                                locSheet.Cell(cRow + cLine, 1).WorksheetColumn().Width = 60;
                                locSheet.Cell(cRow + cLine, 1).Value = com.First().NumeProdus;
                                locSheet.Cell(cRow + cLine, 2).Value = com.Sum(x => x.Cantitate);
                                locSheet.Cell(cRow + cLine, 2).WorksheetColumn().Width = 5;

                                if (cLine % 2 == 1)
                                {
                                    locSheet.Cell(cRow + cLine, 1).Style.Fill.SetBackgroundColor(XLColor.LightGoldenrodYellow);
                                    locSheet.Cell(cRow + cLine, 2).Style.Fill.SetBackgroundColor(XLColor.LightGoldenrodYellow);
                                    if (!string.IsNullOrEmpty(com.Key.l))
                                        locSheet.Cell(cRow + cLine, 3).Style.Fill.SetBackgroundColor(XLColor.LightGoldenrodYellow);
                                }

                                cLine += rowsMereged + 1;
                            }
                            locSheet.Range($"B{cRow}:B{cRow + cLine - 1}").Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
                            cRow += cLine;
                        }
                        SetupWorksheetPage(locSheet, cRow, 3, "nolandscape");
                        //locSheet.Rows().AdjustToContents();
                        //locSheet.Columns("1:2").AdjustToContents();
                    }

                    worksheet2.Rows().AdjustToContents();
                    worksheet2.Columns("1:" + colIndex.Last() + colCount + 1).AdjustToContents();


                    SetupWorksheetPage(worksheet, i + 1, lastColCountIndex);
                    SetupWorksheetPage(worksheet2, colRowIndex.Max() + 1, colIndex.Last() + colCount);

                    workbook.SaveAs(ms);
                    return ms.ToArray();
                }
            }

            int SplitRow(IXLWorksheet worksheet, int cRow, int col, string details, int splitOn = 25, string colsSpanFormat = "A{0}:C{1}")
            {
                if (details.Length / splitOn > 0)
                {
                    //worksheet.Range(string.Format(colsSpanFormat, cRow, cRow + details.Length / splitOn)).Merge();
                    worksheet.Cell(cRow, col).Style.Alignment.WrapText = true;
                }
                worksheet.Cell(cRow, col).Value = details;

                return 0; //details.Length / splitOn;
            }

            void SetupWorksheetPage(IXLWorksheet worksheet, int rowIdx, int colIdx, string pageOrientation = "landscape", bool fitOnePage = false, bool showPages = true)
            {
                worksheet.PageSetup.PrintAreas.Clear();
                worksheet.PageSetup.PrintAreas.Add(1, 1, rowIdx, colIdx);
                worksheet.PageSetup.PageOrientation = pageOrientation == "landscape" ? XLPageOrientation.Landscape : XLPageOrientation.Portrait;
                worksheet.PageSetup.Margins.SetTop(0.7);
                worksheet.PageSetup.Margins.SetLeft(0);
                worksheet.PageSetup.Margins.SetRight(0);
                worksheet.PageSetup.Margins.SetBottom(0);
                worksheet.PageSetup.PaperSize = XLPaperSize.A4Paper;
                worksheet.PageSetup.PagesWide = 1;
                if (fitOnePage)
                {
                    worksheet.PageSetup.PagesTall = 1;
                }
                worksheet.PageSetup.CenterHorizontally = true;

                worksheet.PageSetup.Header.Left.AddText("Artkubika", XLHFOccurrence.AllPages);
                if (showPages)
                {
                    worksheet.PageSetup.Header.Center.AddText(XLHFPredefinedText.PageNumber, XLHFOccurrence.AllPages);
                    worksheet.PageSetup.Header.Center.AddText(" / ", XLHFOccurrence.AllPages);
                    worksheet.PageSetup.Header.Center.AddText(XLHFPredefinedText.NumberOfPages, XLHFOccurrence.AllPages);
                }
                worksheet.PageSetup.Header.Right.AddText(XLHFPredefinedText.Date, XLHFOccurrence.AllPages);
            }

            string GetDisplayName(string s)
            {
                switch (s)
                {
                    case "MPN": return "Noptiera";
                    case "MPP": return "Pat";
                    case "MPS": return "Sifonier";
                    case "MPC": return "Comoda";
                    default: return s;
                }
            }

            string GetSummary(string s)
            {
                if (s.Substring(4, 1) == "N") return "NOPTIERA";
                if (s.Substring(4, 1) == "C") return "COMODA";

                if (s.Substring(4, 1) == "P")
                {
                    if (s.Substring(2, 2) == "KA")
                        return "PAT KARO";
                    else return "PAT ALL/PL/OP";
                }
                if (s.Substring(4, 1) == "S")
                {
                    if (s.Substring(2, 2) == "KA")
                        return $"SIFONIER KARO {s.Substring(7, 1)} USI";
                    if (s.Substring(2, 2) == "OP" || s.Substring(2, 2) == "MR")
                        return "SIFONIER OPERA/MIRA";
                    if (s.Substring(2, 2) == "VY" || s.Substring(2, 2) == "VG")
                        return "SIFONIER VANITY/VG";
                    if (s.Substring(2, 2) == "PL")
                        return "SIFONIER PALLAS";
                    if (s.Substring(2, 2) == "AL")
                        return "SIFONIER ALLEGRO";
                }

                return s;
            }
        }
    }
}
