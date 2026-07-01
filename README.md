# QRQueue

近畿大学マイコン部が作成した抽選システム

## 構成

- **QRQueue** - ASP.NET Core Web API（バックエンド）
- **QRQueue.Desktop** - Avaloniaデスクトップアプリ（レシート印刷用）
- **QRQueue.Aspire** - .NET Aspire開発環境オーケストレーション
- **qrqueue.client** - Svelteフロントエンド

## ディレクトリ構造

```
QRQueue/
├── .github/
│   └── workflows/
│       ├── deploy.yml              # Webアプリデプロイ
│       └── deploy-desktop.yml      # デスクトップアプリデプロイ
├── .deploy/
│   └── qrqueue.service        # systemdサービス定義
├── QRQueue/            # バックエンド
│   ├── Controllers/                # APIコントローラー
│   ├── Models/                     # データモデル
│   ├── Services/                   # ビジネスロジック
│   ├── Hubs/                       # SignalRハブ
│   ├── Migrations/                 # DBマイグレーション
│   ├── qrqueue.client/ # フロントエンド (SvelteKit)
│   └── appsettings.json
├── QRQueue.Desktop/    # デスクトップアプリ
│   ├── Views/                      # Avaloniaビュー
│   ├── ViewModels/                 # MVVM ViewModel
│   ├── Models/                     # データモデル
│   ├── Services/                   # プリンターサービス等
│   ├── Setting/                    # 設定クラス
│   └── appsettings.json
├── QRQueue.Aspire/     # 開発環境オーケストレーション
│   ├── QRQueue.Aspire.AppHost/
│   └── QRQueue.Aspire.ServiceDefaults/
└── QRQueue.sln
```

## 技術スタック

### バックエンド
- .NET 10.0
- ASP.NET Core
- Entity Framework Core
- PostgreSQL

### フロントエンド
- SvelteKit
- TypeScript

### デスクトップアプリ
- .NET 10.0
- Avalonia UI

### 開発環境（.NET Aspire）
- .NET Aspire 9.5

## 開発環境

### 前提ソフトウェア

| ソフトウェア | バージョン | 備考 |
|-------------|-----------|------|
| [.NET SDK](https://dotnet.microsoft.com/download) | 10.0 | `dotnet --version`で確認 |
| [Node.js](https://nodejs.org/) | 20+ | `node --version`で確認（npm同梱） |
| npm | 10+ | `npm --version`で確認 |
| [Docker Desktop](https://www.docker.com/products/docker-desktop) | 最新 | Aspire用コンテナ実行環境 |
| [Git](https://git-scm.com/) | 最新 | `git --version`で確認 |

### インストール確認

```bash
dotnet --version    # 10.0.x
node --version      # v20.x.x
npm --version       # 10.x.x
docker --version    # Docker version ...
git --version       # git version ...
```

### セットアップ

```bash
# リポジトリをクローン
git clone https://github.com/kindai-micon/QRQueue.git
cd QRQueue

# .NETの依存関係を復元
dotnet restore

# フロントエンドの依存関係をインストール
cd QRQueue/qrqueue.client
npm install
cd ../..

# Aspire AppHostを実行（PostgreSQL含む）
dotnet run --project QRQueue.Aspire/QRQueue.Aspire.AppHost

# デスクトップアプリも同時に起動する場合
dotnet run --project QRQueue.Aspire/QRQueue.Aspire.AppHost -- --desktop
```

Aspireダッシュボードが自動的に開き、各サービスの状態を確認できます。

### デスクトップアプリ

抽選券の発行と印刷を行うWindowsデスクトップアプリです。

**主な機能**:
- ログイン・ログアウト
- 抽選会一覧の表示・選択
- 抽選券の発行（指定枚数を一括発行）
- レシートプリンターへの印刷（QRコード付き）
- 発行履歴の管理

```bash
# デスクトップアプリを実行
dotnet run --project QRQueue.Desktop
```

## 設定ファイル

### Webアプリ（appsettings.json）

`QRQueue/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "lottery-db": "Host=localhost;Database=lottery;Port=5432;Username=postgres;Password=your_password"
  },
  "JwtSettings": {
    "SecretKey": "YourSuperSecretKeyForJwtTokenGeneration_Minimum32Characters!",
    "Issuer": "QRQueue",
    "Audience": "QRQueue.Clients",
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 30
  },
  "LotteryBaseUrl": "http://localhost:5000",
  "UseHttpsForQrCode": false,
  "Cors": {
    "AllowedOrigins": ["http://localhost:5000"]
  }
}
```

| 設定項目 | 説明 |
|---------|------|
| `ConnectionStrings:lottery-db` | PostgreSQL接続文字列 |
| `JwtSettings:SecretKey` | JWT署名用秘密鍵（32文字以上） |
| `JwtSettings:Issuer` | JWT発行者 |
| `JwtSettings:Audience` | JWT対象 |
| `LotteryBaseUrl` | 抽選画面のベースURL |
| `UseHttpsForQrCode` | QRコードURLでHTTPSを使用するか |
| `Cors:AllowedOrigins` | CORS許可オリジン |

### デスクトップアプリ（appsettings.json）

`QRQueue.Desktop/appsettings.json`:

```json
{
  "Backend": {
    "Url": "http://localhost:5000"
  },
  "Printer": {
    "PrinterName": "POS-80C",
    "DocumentName": "抽選券印刷",
    "CutEnabled": true
  },
  "ReceiptLayout": {
    "PaperWidthPx": 576,
    "QrSizePx": 220,
    "MarginLeft": 24,
    "MarginRight": 24,
    "MarginTop": 24,
    "MarginBottom": 24,
    "TitleFontSize": 28,
    "NumberFontSize": 42,
    "BodyFontSize": 22,
    "FooterFontSize": 18,
    "Threshold": 160,
    "WarningLines": [
      "本券は大切に保管してください",
      "抽選時までお持ちください"
    ],
    "FooterText": "QRQueue"
  }
}
```

| 設定項目 | 説明 |
|---------|------|
| `Backend:Url` | バックエンドAPIのURL |
| `Printer:PrinterName` | プリンター名 |
| `Printer:CutEnabled` | 自動カット有効/無効 |
| `ReceiptLayout:*` | レシートレイアウト設定 |

## データベース構築

### PostgreSQLのセットアップ

```bash
# PostgreSQLに接続
sudo -u postgres psql

# データベース作成
CREATE DATABASE my_db;

# ユーザー作成
CREATE USER postgres WITH PASSWORD 'your_password';
GRANT ALL PRIVILEGES ON DATABASE my_db TO postgres;
```

### マイグレーション

```bash
# マイグレーションの作成
dotnet ef migrations add InitialCreate --project QRQueue

# データベースに適用
dotnet ef database update --project QRQueue
```

## 本番環境

### 構成

```mermaid
flowchart LR
    A[Internet] --> B[Nginx<br/>リバースプロキシ<br/>SSL終端]
    B --> C[ASP.NET Core<br/>localhost:5000]
    C --> D[(PostgreSQL)]
```

- **Webサーバー**: Nginx（リバースプロキシ、SSL終端）
- **アプリケーションサーバー**: ASP.NET Core (systemd)
- **データベース**: PostgreSQL
- **SSL証明書**: Let's Encrypt (Certbot)
- **デプロイ先**: `/var/www/qrqueue/publish`
- **URL**: https://lottery.kindai-micon.club

### デプロイフロー

1. `release`ブランチにマージ
2. GitHub Actionsが自動的にビルド・デプロイ
3. systemdサービスが再起動

### サービス管理

#### systemdサービス定義

`.deploy/qrqueue.service`:

```ini
[Unit]
Description=QRQueue
After=network.target

[Service]
WorkingDirectory=/var/www/qrqueue/publish
ExecStart=/usr/bin/dotnet /var/www/qrqueue/publish/QRQueue.dll
Restart=always
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://*:5000

[Install]
WantedBy=multi-user.target
```

#### 管理コマンド

```bash
# アプリケーションサービス
sudo systemctl status qrqueue    # 状態確認
sudo systemctl restart qrqueue   # 再起動
sudo journalctl -u qrqueue -f    # ログ確認

# Nginx
sudo systemctl status nginx           # 状態確認
sudo nginx -t                         # 設定テスト
sudo systemctl reload nginx           # 設定再読み込み

# SSL証明書更新
sudo certbot renew                    # 証明書更新
```

## デプロイ

### Webアプリ
`release`ブランチにマージすると自動的にデプロイされます。

### デスクトップアプリ
タグをプッシュするとGitHub Releasesに公開されます。

```bash
git tag v0.1.0
git push origin v0.1.0
```

## ライセンス

[MIT License](LICENSE.txt)
