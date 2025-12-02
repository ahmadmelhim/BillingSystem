using BillingSystem.Core.Interfaces;
using BillingSystem.Core.Interfaces.Repositories;
using BillingSystem.Core.Models;
using Microsoft.AspNetCore.Hosting;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BillingSystem.Infrastructure.Services.Pdf
{
    public class InvoicePdfService : IPdfService
    {
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly IWebHostEnvironment _env;

        // Modern Blue Theme
        private static readonly string PrimaryColor = "#1976D2";
        private static readonly string SecondaryColor = "#424242";
        private static readonly string LightGrey = "#F5F5F5";

        public InvoicePdfService(IInvoiceRepository invoiceRepository, IWebHostEnvironment env)
        {
            _invoiceRepository = invoiceRepository;
            _env = env;
        }

        public async Task<byte[]> GenerateInvoicePdfAsync(int invoiceId)
        {
            // استخدام Repository بدلاً من DbContext
            var invoice = await _invoiceRepository.GetByIdWithDetailsSystemAsync(invoiceId);
            
            if (invoice == null)
                throw new KeyNotFoundException("Invoice not found.");

            // Load Logo
            byte[]? logoBytes = null;
            var logoPath = Path.Combine(_env.WebRootPath, "images", "logo.png");
            if (File.Exists(logoPath))
                logoBytes = await File.ReadAllBytesAsync(logoPath);

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.DefaultTextStyle(x => x.FontSize(10).FontColor(SecondaryColor));

                    page.Header().Element(header => ComposeHeader(header, invoice, logoBytes));
                    page.Content().Element(content => ComposeContent(content, invoice));
                    page.Footer().Element(footer => ComposeFooter(footer));
                });
            });

            return document.GeneratePdf();
        }

        private void ComposeHeader(IContainer container, Invoice invoice, byte[]? logoBytes)
        {
            container.Row(row =>
            {
                // Logo and Company Info
                row.RelativeItem().Column(stack =>
                {
                    if (logoBytes != null)
                    {
                        stack.Item().Height(60).Image(logoBytes).FitArea();
                    }
                    else
                    {
                        stack.Item().Text("Billing System").FontSize(24).SemiBold().FontColor(PrimaryColor);
                    }
                });

                // Invoice Title and Number
                row.RelativeItem().AlignRight().Column(stack =>
                {
                    stack.Item().Text("INVOICE").FontSize(32).Bold().FontColor(PrimaryColor);
                    stack.Item().Text($"#{invoice.InvoiceNumber}").FontSize(16).SemiBold();
                    stack.Item().PaddingTop(10).Text($"Date: {invoice.DateIssued:MMM dd, yyyy}");
                    
                    if (invoice.DueDate.HasValue)
                        stack.Item().Text($"Due: {invoice.DueDate:MMM dd, yyyy}").FontColor(Colors.Red.Medium);
                        
                    stack.Item().Text($"Status: {invoice.Status}").SemiBold();
                });
            });
        }

        private void ComposeContent(IContainer container, Invoice invoice)
        {
            container.PaddingVertical(30).Column(stack =>
            {
                // Customer Details Section
                stack.Item().Row(row =>
                {
                    row.RelativeItem().Column(s =>
                    {
                        s.Item().Text("Bill To:").FontSize(12).SemiBold().FontColor(PrimaryColor);
                        s.Item().Text(invoice.Customer?.Name ?? "N/A").FontSize(14).Bold();
                        
                        if (!string.IsNullOrWhiteSpace(invoice.Customer?.Email))
                            s.Item().Text(invoice.Customer.Email);
                            
                        if (!string.IsNullOrWhiteSpace(invoice.Customer?.Phone))
                            s.Item().Text(invoice.Customer.Phone);
                            
                        if (!string.IsNullOrWhiteSpace(invoice.Customer?.Address))
                            s.Item().Text(invoice.Customer.Address);
                    });
                });

                stack.Item().PaddingTop(30);

                // Items Table
                stack.Item().Table(table =>
                {
                    // Definition
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(4); // Description
                        columns.RelativeColumn(1); // Qty
                        columns.RelativeColumn(2); // Unit Price
                        columns.RelativeColumn(2); // Total
                    });

                    // Header
                    table.Header(header =>
                    {
                        header.Cell().Element(CellStyle).Background(PrimaryColor).Padding(10).Text("Description").FontColor(Colors.White).SemiBold();
                        header.Cell().Element(CellStyle).Background(PrimaryColor).Padding(10).AlignRight().Text("Qty").FontColor(Colors.White).SemiBold();
                        header.Cell().Element(CellStyle).Background(PrimaryColor).Padding(10).AlignRight().Text("Unit Price").FontColor(Colors.White).SemiBold();
                        header.Cell().Element(CellStyle).Background(PrimaryColor).Padding(10).AlignRight().Text("Total").FontColor(Colors.White).SemiBold();

                        static IContainer CellStyle(IContainer c) => c.BorderBottom(1).BorderColor(PrimaryColor);
                    });

                    // Rows
                    foreach (var item in invoice.Items)
                    {
                        table.Cell().Element(CellStyle).Text(item.Description);
                        table.Cell().Element(CellStyle).AlignRight().Text(item.Quantity.ToString("0.##"));
                        table.Cell().Element(CellStyle).AlignRight().Text($"₪{item.UnitPrice:N2}");
                        table.Cell().Element(CellStyle).AlignRight().Text($"₪{item.TotalPrice:N2}").SemiBold();

                        static IContainer CellStyle(IContainer c) => c.BorderBottom(1).BorderColor(LightGrey).Padding(10);
                    }
                });

                // Totals Section
                stack.Item().PaddingTop(20).Row(row =>
                {
                    row.RelativeItem(2); // Spacer
                    row.RelativeItem(1).Column(s =>
                    {
                        s.Item().Row(r =>
                        {
                            r.RelativeItem().Text("Total Amount:").FontSize(14).SemiBold().AlignRight();
                            r.RelativeItem().PaddingLeft(10).Text($"₪{invoice.TotalAmount:N2}").FontSize(14).Bold().FontColor(PrimaryColor).AlignRight();
                        });
                    });
                });
            });
        }

        private void ComposeFooter(IContainer container)
        {
            container.PaddingTop(20).Column(column =>
            {
                column.Item().LineHorizontal(1).LineColor(LightGrey);
                column.Item().PaddingTop(10).Row(row =>
                {
                    row.RelativeItem().Text(x =>
                    {
                        x.Span("Thank you for your business!").SemiBold();
                    });
                    
                    row.RelativeItem().AlignRight().Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                        x.Span(" of ");
                        x.TotalPages();
                    });
                });
            });
        }
    }
}
