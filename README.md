Yayınladığım rest api ardından onun servisini sağlayan api consumer uygulamasıdır. bu uygulama apiye erişim sağlayarak elde ettiğimiz json verisini gerekli sql komutları ile veri tabanımızdaki mevcut olan urunBarkod değerlerinin urunFiyat ve urunIsim değerlerlerini
kontrol eder ve eğer elde edilen veri ile veri tabanında bir değişiklik veya uyuşmazlık varsa eşleşen barkod değerini jsondan elde ettiğimiz güncel veri ile değiştiririz. uygulamanın çalıştığı sql tablosu ise : 
CREATE TABLE `etiket` (
  `etiketID` int NOT NULL AUTO_INCREMENT,
  `urunBarkod` varchar(45) DEFAULT NULL,
  `urunIsim` varchar(45) DEFAULT NULL,
  `urunFiyat` decimal(18,2) DEFAULT NULL,
  PRIMARY KEY (`etiketID`)
) ENGINE=InnoDB AUTO_INCREMENT=3 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

örnek olarak api çalışması :

Rest api outputu :
![image](https://github.com/user-attachments/assets/00656c79-cfc0-4cea-b6a0-00b0b71e2bc3)

Api consumer outputu :
![image](https://github.com/user-attachments/assets/421733f5-7626-4667-9e94-e2bb2bb08355)


güncellenen ve güncellenmeyen değerlerin barkodu ekrana yansıtılmaktadır.
