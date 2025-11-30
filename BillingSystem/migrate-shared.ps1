# Shared Components Migration Script
$projectRoot = "c:\Users\moham\source\repos\BillingSystem\BillingSystem\BillingSystem"
Set-Location $projectRoot

Write-Host "Starting Shared Components Migration..." -ForegroundColor Green

# ===== Shared\Layouts =====
Write-Host "`n[1/3] Moving Layouts..." -ForegroundColor Cyan
$layouts = @("MainLayout.razor", "MainLayout.razor.css", "EmptyLayout.razor", "NavMenu.razor", "NavMenu.razor.css")
foreach ($file in $layouts) {
    Copy-Item "Shared\$file" "Shared\Layouts\$file" -Force
}

# Update namespaces in layouts
$content = Get-Content "Shared\Layouts\MainLayout.razor" -Raw
$content = $content -replace "@using BillingSystem\.Services", "@using BillingSystem.Infrastructure.Services.Auth`n@using BillingSystem.Infrastructure.Services.Localization"
Set-Content "Shared\Layouts\MainLayout.razor" $content

# ===== Shared\Components =====
Write-Host "[2/3] Moving Shared Components..." -ForegroundColor Cyan
$components = @("ConfirmDialog.razor", "LanguageSwitcher.razor", "SurveyPrompt.razor")
foreach ($file in $components) {
    Copy-Item "Shared\$file" "Shared\Components\$file" -Force
}

$content = Get-Content "Shared\Components\LanguageSwitcher.razor" -Raw
$content = $content -replace "@using BillingSystem\.Services", "@using BillingSystem.Infrastructure.Services.Localization"
Set-Content "Shared\Components\LanguageSwitcher.razor" $content

# ===== Examples =====
Write-Host "[3/3] Moving Example files..." -ForegroundColor Cyan
Copy-Item "Pages\Counter.razor" "Examples\Counter.razor" -Force
Copy-Item "Pages\FetchData.razor" "Examples\FetchData.razor" -Force
Copy-Item "Data\WeatherForecast.cs" "Examples\WeatherForecast.cs" -Force
Copy-Item "Data\WeatherForecastService.cs" "Examples\WeatherForecastService.cs" -Force

# Update namespaces in Examples
$content = Get-Content "Examples\WeatherForecast.cs" -Raw
$content = $content -replace "namespace BillingSystem\.Data;", "namespace BillingSystem.Examples;"
Set-Content "Examples\WeatherForecast.cs" $content

$content = Get-Content "Examples\WeatherForecastService.cs" -Raw
$content = $content -replace "using BillingSystem\.Data;", "using BillingSystem.Examples;"
$content = $content -replace "namespace BillingSystem\.Data;", "namespace BillingSystem.Examples;"
Set-Content "Examples\WeatherForecastService.cs" $content

$content = Get-Content "Examples\FetchData.razor" -Raw
$content = $content -replace "@using BillingSystem\.Data", "@using BillingSystem.Examples"
Set-Content "Examples\FetchData.razor" $content

Write-Host "`nShared components migration completed!" -ForegroundColor Green
Write-Host "All file migrations complete! Next: Update _Imports.razor and Program.cs" -ForegroundColor Yellow
