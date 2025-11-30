# Project Reorganization Script
# This script moves files to new structure and updates namespaces

$projectRoot = "c:\Users\moham\source\repos\BillingSystem\BillingSystem\BillingSystem"
Set-Location $projectRoot

Write-Host "Starting Project Reorganization..." -ForegroundColor Green

# ===== Infrastructure\Data =====
Write-Host "`n[1/8] Moving Infrastructure\Data files..." -ForegroundColor Cyan
Copy-Item "Data\ApplicationDbContext.cs" "Infrastructure\Data\ApplicationDbContext.cs" -Force
Copy-Item "Migrations" "Infrastructure\Data\Migrations" -Recurse -Force

# Update namespace in ApplicationDbContext
$content = Get-Content "Infrastructure\Data\ApplicationDbContext.cs" -Raw
$content = $content -replace "using BillingSystem\.Models;", "using BillingSystem.Core.Models;"
$content = $content -replace "namespace BillingSystem\.Data;", "namespace BillingSystem.Infrastructure.Data;"
Set-Content "Infrastructure\Data\ApplicationDbContext.cs" $content

# ===== Infrastructure\Services\Auth =====
Write-Host "[2/8] Moving Infrastructure\Services\Auth files..." -ForegroundColor Cyan
$authFiles = @("AuthService.cs", "CustomAuthenticationStateProvider.cs", "TokenService.cs")
foreach ($file in $authFiles) {
    Copy-Item "Services\$file" "Infrastructure\Services\Auth\$file" -Force
    $content = Get-Content "Infrastructure\Services\Auth\$file" -Raw
    $content = $content -replace "using BillingSystem\.Models;", "using BillingSystem.Core.Models;`nusing BillingSystem.Core.DTOs;"
    $content = $content -replace "namespace BillingSystem\.Services;", "namespace BillingSystem.Infrastructure.Services.Auth;"
    Set-Content "Infrastructure\Services\Auth\$file" $content
}

# ===== Infrastructure\Services\Business =====
Write-Host "[3/8] Moving Infrastructure\Services\Business files..." -ForegroundColor Cyan
$businessFiles = @("CustomerService.cs", "InvoiceService.cs", "PaymentService.cs", "ReportService.cs", "UserService.cs")
foreach ($file in $businessFiles) {
    Copy-Item "Services\$file" "Infrastructure\Services\Business\$file" -Force
    $content = Get-Content "Infrastructure\Services\Business\$file" -Raw
    $content = $content -replace "using BillingSystem\.Models;", "using BillingSystem.Core.Models;"
    $content = $content -replace "using BillingSystem\.Models\.Reports;", "using BillingSystem.Core.DTOs;"
    $content = $content -replace "using BillingSystem\.Services;", "using BillingSystem.Core.Interfaces;"
    $content = $content -replace "using BillingSystem\.Data;", "using BillingSystem.Infrastructure.Data;"
    $content = $content -replace "namespace BillingSystem\.Services;", "namespace BillingSystem.Infrastructure.Services.Business;"
    Set-Content "Infrastructure\Services\Business\$file" $content
}

# ===== Infrastructure\Services\Email =====
Write-Host "[4/8] Moving Infrastructure\Services\Email files..." -ForegroundColor Cyan
$emailFiles = @("EmailService.cs", "EmailTemplateHelper.cs")
foreach ($file in $emailFiles) {
    Copy-Item "Services\$file" "Infrastructure\Services\Email\$file" -Force
    $content = Get-Content "Infrastructure\Services\Email\$file" -Raw
    $content = $content -replace "using BillingSystem\.Services;", "using BillingSystem.Core.Interfaces;`nusing BillingSystem.Infrastructure.Configuration;"
    $content = $content -replace "namespace BillingSystem\.Services", "namespace BillingSystem.Infrastructure.Services.Email"
    Set-Content "Infrastructure\Services\Email\$file" $content
}

# ===== Infrastructure\Services\Pdf =====
Write-Host "[5/8] Moving Infrastructure\Services\Pdf files..." -ForegroundColor Cyan
Copy-Item "Services\InvoicePdfService.cs" "Infrastructure\Services\Pdf\InvoicePdfService.cs" -Force
$content = Get-Content "Infrastructure\Services\Pdf\InvoicePdfService.cs" -Raw
$content = $content -replace "using BillingSystem\.Models;", "using BillingSystem.Core.Models;"
$content = $content -replace "using BillingSystem\.Services;", "using BillingSystem.Core.Interfaces;"
$content = $content -replace "using BillingSystem\.Data;", "using BillingSystem.Infrastructure.Data;"
$content = $content -replace "namespace BillingSystem\.Services", "namespace BillingSystem.Infrastructure.Services.Pdf"
Set-Content "Infrastructure\Services\Pdf\InvoicePdfService.cs" $content

# ===== Infrastructure\Services\Localization =====
Write-Host "[6/8] Moving Infrastructure\Services\Localization files..." -ForegroundColor Cyan
Copy-Item "Services\LanguageService.cs" "Infrastructure\Services\Localization\LanguageService.cs" -Force
$content = Get-Content "Infrastructure\Services\Localization\LanguageService.cs" -Raw
$content = $content -replace "namespace BillingSystem\.Services", "namespace BillingSystem.Infrastructure.Services.Localization"
Set-Content "Infrastructure\Services\Localization\LanguageService.cs" $content

# ===== Infrastructure\BackgroundServices =====
Write-Host "[7/8] Moving Infrastructure\BackgroundServices files..." -ForegroundColor Cyan
Copy-Item "Services\OverdueInvoiceWorker.cs" "Infrastructure\BackgroundServices\OverdueInvoiceWorker.cs" -Force
$content = Get-Content "Infrastructure\BackgroundServices\OverdueInvoiceWorker.cs" -Raw
$content = $content -replace "using BillingSystem\.Data;", "using BillingSystem.Infrastructure.Data;"
$content = $content -replace "namespace BillingSystem\.Services", "namespace BillingSystem.Infrastructure.BackgroundServices"
Set-Content "Infrastructure\BackgroundServices\OverdueInvoiceWorker.cs" $content

# ===== Infrastructure\Configuration =====
Write-Host "[8/8] Moving Infrastructure\Configuration files..." -ForegroundColor Cyan
$configFiles = @("EmailSettings.cs", "JwtSettings.cs")
foreach ($file in $configFiles) {
    Copy-Item "Services\$file" "Infrastructure\Configuration\$file" -Force
    $content = Get-Content "Infrastructure\Configuration\$file" -Raw
    $content = $content -replace "namespace BillingSystem\.Services", "namespace BillingSystem.Infrastructure.Configuration"
    Set-Content "Infrastructure\Configuration\$file" $content
}

Write-Host "`nInfrastructure migration completed!" -ForegroundColor Green
Write-Host "Next: Run the Features migration script..." -ForegroundColor Yellow
