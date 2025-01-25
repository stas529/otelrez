# Otel Rezervasyon Sistemi - Dönem Sonu Projesi

Windows Forms tabanlı otel rezervasyon yönetim sistemi. C# ve MySQL Server kullanılarak geliştirilmiştir.

*Proje Tanıtım Videosu*:

[![Proje Demo](https://img.youtube.com/vi/X22Sbf-z2EI/maxresdefault.jpg)](https://www.youtube.com/watch?v=X22Sbf-z2EI)


## Özellikler

- Oda arama ve rezervasyon
- Rezervasyon görüntüleme/iptal
- TC Kimlik ile işlem
- Oda yönetimi (CRUD)
- Müşteri/rezervasyon takibi
- Giriş-çıkış işlemleri
  
### Güvenlik & Doğrulamalar
- TC Kimlik doğrulama (11 haneli numerik kontrol)
- Brute Force Koruması 
- Yetkilendirme kontrolleri

### Veri Kontrolleri
- Tarih validasyonları (geçmiş tarih kontrolü)
- Fiyat formatı kontrolü (negatif değer engelleme)
- Boş alan kontrolleri
- Telefon format kontrolü
- Email format doğrulama

### Rezervasyon Sistemi
- Müsaitlik kontrolü
- Otomatik fiyat hesaplama
- Oda tipi bazlı fiyatlandırma
  
![45_21-YoneticiForm](https://github.com/user-attachments/assets/17e846d3-8222-4eea-85eb-9da221541d31)
![44_15-Rezervasyon Detay - #3](https://github.com/user-attachments/assets/54c9c80d-5daa-42f9-983a-2c96dc3f4912)

### Faturalandırma Sistemi
- Otomatik fatura üretme
- PDF formatında çıktı alma
- Fatura geçmişi görüntüleme
  
### Oda Yönetimi
- Oda tipleri (Standart, Deluxe, Suite)
- Durum takibi (Boş, Dolu, Temizlik, Bakım)
  
![44_08-YoneticiForm](https://github.com/user-attachments/assets/43da1343-b445-40c7-b055-20c652b19008)

##  Diyagramlar 

ER Diyagramı

![erd](https://github.com/user-attachments/assets/6b3f3f2d-32f8-4cb4-a329-0f0703bfdddf)

Use case diyagramı
 
![use case](https://github.com/user-attachments/assets/44f16d11-a472-48b3-be18-7ddad53de93d)

Class diagram
  
![class diagram](https://github.com/user-attachments/assets/74b40a84-ed89-479a-a338-056fe2515b95)
#

### Katmanlı Mimari (3-Tier)
- Presentation Layer (Forms)
- Business Layer (Logic)
- Data Access Layer (DB)

### Veritabanı Yapısı
- Otel
- Oda (Base class)
- OzelOda (Inherited)
- Musteri
- Rezervasyon
- Fatura
- Yonetici

### OOP Prensipleri
1. **Inheritance (Kalıtım)**
   - Base Class: Oda
   - Derived Class: OzelOda

2. **Encapsulation (Kapsülleme)**
   - Private fields
   - Public properties
   - Veri doğrulama

3. **Abstraction (Soyutlama)**
   - Abstract class: Kullanici
   - Interface: IOdemeYontemi

4. **Polymorphism (Çok Biçimlilik)**
   - Method overriding (FiyatHesapla)
   - Interface kullanımı

## Kurulum
1. Visual Studio ile `OtelRezervasyon.sln` yi çalıştırın

## Geliştirici
- GitHub: [@stas529](https://github.com/stas529)
