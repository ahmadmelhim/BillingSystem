# Features Migration Script
# This script moves Pages and Components to feature-based organization

$projectRoot = "c:\Users\moham\source\repos\BillingSystem\BillingSystem\BillingSystem"
Set-Location $projectRoot

Write-Host "Starting Features Migration..." -ForegroundColor Green

# ===== Authentication =====
Write-Host "`n[1/7] Moving Authentication feature..." -ForegroundColor Cyan
Copy-Item "Pages\Login.razor" "Features\Authentication\Pages\Login.razor" -Force
Copy-Item "Pages\Register.razor" "Features\Authentication\Pages\Register.razor" -Force
Copy-Item "Shared\RedirectToLogin.razor" "Features\Authentication\Components\RedirectToLogin.razor" -Force

# Update namespaces
$authPages = @("Login.razor", "Register.razor")
foreach ($page in $authPages) {
    $content = Get-Content "Features\Authentication\Pages\$page" -Raw
    $content = $content -replace "@using BillingSystem\.Models", "@using BillingSystem.Core.Models`n@using BillingSystem.Core.DTOs"
    $content = $content -replace "@using BillingSystem\.Services", "@using BillingSystem.Infrastructure.Services.Auth"
    Set-Content "Features\Authentication\Pages\$page" $content
}

# ===== Customers =====
Write-Host "[2/7] Moving Customers feature..." -ForegroundColor Cyan
Copy-Item "Pages\Customers.razor" "Features\Customers\Pages\Customers.razor" -Force
Copy-Item "Pages\CustomerEdit.razor" "Features\Customers\Pages\CustomerEdit.razor" -Force

$custPages = @("Customers.razor", "CustomerEdit.razor")
foreach ($page in $custPages) {
    $content = Get-Content "Features\Customers\Pages\$page" -Raw
    $content = $content -replace "@using BillingSystem\.Models", "@using BillingSystem.Core.Models"
    $content = $content -replace "@using BillingSystem\.Services", "@using BillingSystem.Core.Interfaces"
    Set-Content "Features\Customers\Pages\$page" $content
}

# ===== Invoices =====
Write-Host "[3/7] Moving Invoices feature..." -ForegroundColor Cyan
Copy-Item "Pages\Invoices.razor" "Features\Invoices\Pages\Invoices.razor" -Force
Copy-Item "Pages\InvoiceEdit.razor" "Features\Invoices\Pages\InvoiceEdit.razor" -Force

$invPages = @("Invoices.razor", "InvoiceEdit.razor")
foreach ($page in $invPages) {
    $content = Get-Content "Features\Invoices\Pages\$page" -Raw
    $content = $content -replace "@using BillingSystem\.Models", "@using BillingSystem.Core.Models"
    $content = $content -replace "@using BillingSystem\.Services", "@using BillingSystem.Core.Interfaces"
    Set-Content "Features\Invoices\Pages\$page" $content
}

# ===== Payments =====
Write-Host "[4/7] Moving Payments feature..." -ForegroundColor Cyan
Copy-Item "Pages\Payments.razor" "Features\Payments\Pages\Payments.razor" -Force
Copy-Item "Pages\AddPaymentDialog.razor" "Features\Payments\Components\AddPaymentDialog.razor" -Force

$payPages = @("Payments.razor")
foreach ($page in $payPages) {
    $content = Get-Content "Features\Payments\Pages\$page" -Raw
    $content = $content -replace "@using BillingSystem\.Models", "@using BillingSystem.Core.Models"
    $content = $content -replace "@using BillingSystem\.Services", "@using BillingSystem.Core.Interfaces"
    Set-Content "Features\Payments\Pages\$page" $content
}

$content = Get-Content "Features\Payments\Components\AddPaymentDialog.razor" -Raw
$content = $content -replace "@using BillingSystem\.Models", "@using BillingSystem.Core.Models"
$content = $content -replace "@using BillingSystem\.Services", "@using BillingSystem.Core.Interfaces"
Set-Content "Features\Payments\Components\AddPaymentDialog.razor" $content

# ===== Users =====
Write-Host "[5/7] Moving Users feature..." -ForegroundColor Cyan
Copy-Item "Pages\Users.razor" "Features\Users\Pages\Users.razor" -Force
Copy-Item "Pages\UserEditDialog.razor" "Features\Users\Components\UserEditDialog.razor" -Force

$content = Get-Content "Features\Users\Pages\Users.razor" -Raw
$content = $content -replace "@using BillingSystem\.Models", "@using BillingSystem.Core.Models"
$content = $content -replace "@using BillingSystem\.Services", "@using BillingSystem.Core.Interfaces"
Set-Content "Features\Users\Pages\Users.razor" $content

$content = Get-Content "Features\Users\Components\UserEditDialog.razor" -Raw
$content = $content -replace "@using BillingSystem\.Models", "@using BillingSystem.Core.Models"
Set-Content "Features\Users\Components\UserEditDialog.razor" $content

# ===== Reports =====
Write-Host "[6/7] Moving Reports feature..." -ForegroundColor Cyan
Copy-Item "Pages\Reports\CustomersReport.razor" "Features\Reports\Pages\CustomersReport.razor" -Force
Copy-Item "Pages\Reports\InvoicesReport.razor" "Features\Reports\Pages\InvoicesReport.razor" -Force
Copy-Item "Pages\Reports\PaymentsReport.razor" "Features\Reports\Pages\PaymentsReport.razor" -Force

$repPages = @("CustomersReport.razor", "InvoicesReport.razor", "PaymentsReport.razor")
foreach ($page in $repPages) {
    $content = Get-Content "Features\Reports\Pages\$page" -Raw
    $content = $content -replace "@using BillingSystem\.Models", "@using BillingSystem.Core.Models"
    $content = $content -replace "@using BillingSystem\.Models\.Reports", "@using BillingSystem.Core.DTOs"
    $content = $content -replace "@using BillingSystem\.Services", "@using BillingSystem.Core.Interfaces"
    Set-Content "Features\Reports\Pages\$page" $content
}

# ===== Dashboard =====
Write-Host "[7/7] Moving Dashboard feature..." -ForegroundColor Cyan
Copy-Item "Pages\Index.razor" "Features\Dashboard\Pages\Index.razor" -Force
Copy-Item "Pages\Dashboard.razor" "Features\Dashboard\Pages\Dashboard.razor" -Force

$dashPages = @("Index.razor", "Dashboard.razor")
foreach ($page in $dashPages) {
    $content = Get-Content "Features\Dashboard\Pages\$page" -Raw
    $content = $content -replace "@using BillingSystem\.Models", "@using BillingSystem.Core.Models"
    $content = $content -replace "@using BillingSystem\.Services", "@using BillingSystem.Core.Interfaces`n@using BillingSystem.Infrastructure.Services.Localization"
    Set-Content "Features\Dashboard\Pages\$page" $content
}

Write-Host "`nFeatures migration completed!" -ForegroundColor Green
Write-Host "Next: Run the Shared components migration..." -ForegroundColor Yellow
