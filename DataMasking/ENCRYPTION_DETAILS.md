# TÀI LIỆU CHI TIẾT VỀ HỆ THỐNG MÃ HÓA DỮ LIỆU

## Tổng quan hệ thống

Hệ thống sử dụng kết hợp hai thuật toán mã hóa:
- **RSA (Rivest-Shamir-Adleman)**: Mã hóa bất đối xứng để trao đổi khóa an toàn
- **AES-256 (Advanced Encryption Standard)**: Mã hóa đối xứng để mã hóa dữ liệu thực tế

### Quy trình tổng thể:
1. Server tạo cặp khóa RSA (Public Key + Private Key)
2. Client nhận Public Key của Server
3. Client tạo khóa AES ngẫu nhiên
4. Client mã hóa dữ liệu bằng AES
5. Client mã hóa khóa AES bằng RSA Public Key
6. Client gửi: [Dữ liệu đã mã hóa AES] + [Khóa AES đã mã hóa RSA] + [IV]
7. Server giải mã khóa AES bằng RSA Private Key
8. Server giải mã dữ liệu bằng khóa AES vừa giải mã được

---

## PHẦN 1: THUẬT TOÁN RSA

### 1.1. Khái niệm cơ bản

RSA là thuật toán mã hóa bất đối xứng, sử dụng hai khóa khác nhau:
- **Public Key (n, e)**: Dùng để mã hóa, có thể công khai
- **Private Key (n, d)**: Dùng để giải mã, phải giữ bí mật

### 1.2. Quá trình tạo khóa RSA (KeyPair.cs)

#### Bước 1: Tạo hai số nguyên tố lớn p và q
```csharp
P = BigMath.GeneratePrime(keySize / 2);  // Ví dụ: 512 bit
Q = BigMath.GeneratePrime(keySize / 2);  // Ví dụ: 512 bit
```

**Cách tạo số nguyên tố (BigMath.cs - GeneratePrime):**

1. Tạo số ngẫu nhiên có độ dài `bits` bit
2. Đặt bit cao nhất = 1 (đảm bảo số đủ lớn)
3. Đặt bit thấp nhất = 1 (đảm bảo số lẻ)
4. Kiểm tra tính nguyên tố bằng **Miller-Rabin test**
5. Nếu không phải số nguyên tố, lặp lại từ bước 1

**Miller-Rabin Test (BigMath.cs - IsPrime):**
- Thuật toán xác suất để kiểm tra số nguyên tố
- Độ chính xác: với k=10 vòng lặp, xác suất sai < 1/(2^20)
- Nguyên lý:
  - Phân tích n-1 = 2^r × d (d lẻ)
  - Chọn ngẫu nhiên a trong [2, n-2]
  - Tính x = a^d mod n
  - Kiểm tra điều kiện Fermat và căn bậc hai của 1

#### Bước 2: Tính modulus n
```csharp
N = P * Q;  // Ví dụ: nếu p và q là 512 bit, n sẽ là 1024 bit
```

**Ý nghĩa:** n là modulus dùng trong cả mã hóa và giải mã. Độ an toàn của RSA phụ thuộc vào việc phân tích n thành p và q là rất khó.

#### Bước 3: Tính hàm Euler φ(n)
```csharp
BigInteger phi = (P - 1) * (Q - 1);
```

**Ý nghĩa:** φ(n) là số lượng số nguyên dương nhỏ hơn n và nguyên tố cùng nhau với n. Với n = p×q (p, q nguyên tố), φ(n) = (p-1)(q-1).

#### Bước 4: Chọn số mũ công khai e
```csharp
E = 65537;  // Số Fermat F4 = 2^16 + 1
while (BigMath.GCD(E, phi) != 1)
{
    E += 2;
}
```

**Tại sao chọn 65537?**
- Là số nguyên tố
- Có dạng nhị phân: 10000000000000001 (chỉ có 2 bit 1)
- Làm cho phép mã hóa nhanh hơn (ít phép nhân hơn)
- Đủ lớn để an toàn

**Điều kiện:** GCD(e, φ(n)) = 1 (e và φ(n) nguyên tố cùng nhau)

#### Bước 5: Tính số mũ bí mật d
```csharp
D = BigMath.ModInverse(E, phi);
```

**Công thức:** d ≡ e^(-1) (mod φ(n)), tức là: (e × d) mod φ(n) = 1

**Thuật toán Extended Euclidean (BigMath.cs - ModInverse):**

```
Tìm x sao cho: a×x ≡ 1 (mod m)
Sử dụng thuật toán Euclid mở rộng:
- Tìm GCD(a, m) và các hệ số x, y sao cho: a×x + m×y = GCD(a, m)
- Nếu GCD(a, m) = 1, thì x chính là nghịch đảo modulo
```

**Kết quả:**
- **Public Key:** (n, e) - Có thể công khai
- **Private Key:** (n, d) - Phải giữ bí mật
- **Các số p, q, φ(n):** Phải hủy sau khi tạo khóa

### 1.3. Mã hóa RSA (RSA.cs - Encrypt)

**Công thức:** C = M^e mod n

Trong đó:
- M: Message (dữ liệu gốc dạng số)
- C: Ciphertext (dữ liệu đã mã hóa)
- e, n: Public Key

**Quy trình:**
```csharp
// 1. Chuyển byte array thành BigInteger
byte[] paddedData = new byte[data.Length + 1];
Array.Copy(data, paddedData, data.Length);
paddedData[data.Length] = 0;  // Thêm byte 0 để đảm bảo số dương
BigInteger message = new BigInteger(paddedData);

// 2. Kiểm tra message < n (điều kiện bắt buộc)
if (message >= n)
    throw new ArgumentException("Dữ liệu quá lớn");

// 3. Tính C = M^e mod n
BigInteger encrypted = BigMath.ModPow(message, e, n);

// 4. Chuyển về byte array
return encrypted.ToByteArray();
```

**Thuật toán ModPow (Modular Exponentiation):**
```
Tính: base^exp mod m
Sử dụng phương pháp "Square and Multiply":
result = 1
while exp > 0:
    if exp là số lẻ:
        result = (result × base) mod m
    base = (base × base) mod m
    exp = exp / 2
return result
```

**Ví dụ:** Tính 5^13 mod 7
```
13 = 1101 (nhị phân)
Bước 1: exp=13 (lẻ) → result = 1×5 = 5, base = 5×5 = 25 mod 7 = 4, exp = 6
Bước 2: exp=6 (chẵn) → base = 4×4 = 16 mod 7 = 2, exp = 3
Bước 3: exp=3 (lẻ) → result = 5×2 = 10 mod 7 = 3, base = 2×2 = 4, exp = 1
Bước 4: exp=1 (lẻ) → result = 3×4 = 12 mod 7 = 5
Kết quả: 5
```

**Tại sao không tính trực tiếp M^e rồi mod n?**
- M^e có thể cực kỳ lớn (hàng nghìn bit)
- ModPow giữ số luôn nhỏ bằng cách mod sau mỗi phép nhân
- Hiệu quả: O(log e) phép nhân thay vì O(e)

### 1.4. Giải mã RSA (RSA.cs - Decrypt)

**Công thức:** M = C^d mod n

Trong đó:
- C: Ciphertext (dữ liệu đã mã hóa)
- M: Message (dữ liệu gốc)
- d, n: Private Key

**Quy trình:**

```csharp
// 1. Chuyển byte array thành BigInteger
BigInteger encrypted = new BigInteger(data);

// 2. Đảm bảo số dương
if (encrypted < 0)
    encrypted = BigInteger.Abs(encrypted);

// 3. Tính M = C^d mod n
BigInteger decrypted = BigMath.ModPow(encrypted, d, n);

// 4. Chuyển về byte array và loại bỏ padding
byte[] result = decrypted.ToByteArray();
// Loại bỏ byte 0 cuối cùng (đã thêm khi mã hóa)
```

**Tại sao RSA hoạt động?**

Định lý Euler: Nếu GCD(M, n) = 1, thì M^φ(n) ≡ 1 (mod n)

Chứng minh:
```
C^d = (M^e)^d = M^(e×d) (mod n)
Vì e×d ≡ 1 (mod φ(n)), nên e×d = 1 + k×φ(n) với k nguyên
M^(e×d) = M^(1 + k×φ(n)) = M × (M^φ(n))^k ≡ M × 1^k = M (mod n)
```

### 1.5. Giới hạn kích thước dữ liệu RSA

**Vấn đề:** RSA chỉ mã hóa được dữ liệu nhỏ hơn modulus n

Ví dụ: RSA-1024 (n có 1024 bit = 128 bytes)
- Dữ liệu tối đa: ~117 bytes (trừ padding)
- Không phù hợp để mã hóa dữ liệu lớn

**Giải pháp trong code:**
1. **Mã hóa từng block nhỏ** (EncryptLarge/DecryptLarge)
2. **Hybrid encryption:** Dùng RSA mã hóa khóa AES, dùng AES mã hóa dữ liệu ← Cách này được dùng trong hệ thống

---

## PHẦN 2: THUẬT TOÁN AES-256

### 2.1. Khái niệm cơ bản

AES (Advanced Encryption Standard) là thuật toán mã hóa đối xứng:
- **Đối xứng:** Cùng một khóa để mã hóa và giải mã
- **Block cipher:** Mã hóa từng block 128 bit (16 bytes)
- **AES-256:** Sử dụng khóa 256 bit (32 bytes)

**Cấu trúc:**
- Input: 128-bit block (ma trận 4×4 bytes)
- Key: 256-bit (32 bytes)
- Output: 128-bit block (ma trận 4×4 bytes)
- Số vòng (rounds): 14 vòng cho AES-256

### 2.2. Tạo khóa AES (KeyPair.cs - AESKey)

```csharp
public AESKey()
{
    Key = new byte[32]; // AES-256: 32 bytes = 256 bits
    IV = new byte[16];  // IV: 16 bytes = 128 bits
    
    Random rand = new Random();
    rand.NextBytes(Key);  // Tạo khóa ngẫu nhiên
    rand.NextBytes(IV);   // Tạo IV ngẫu nhiên
}
```

**Thành phần:**
1. **Key (32 bytes):** Khóa bí mật dùng để mã hóa/giải mã
2. **IV - Initialization Vector (16 bytes):** Vector khởi tạo cho CBC mode

**Tại sao cần IV?**
- Đảm bảo cùng plaintext với cùng key sẽ tạo ra ciphertext khác nhau
- Ngăn chặn pattern analysis attack
- IV không cần bí mật, nhưng phải unique cho mỗi lần mã hóa
- IV được gửi kèm ciphertext (không mã hóa)

### 2.3. Ma trận State (4×4 bytes)

AES xử lý dữ liệu dưới dạng ma trận 4×4 bytes (16 bytes = 128 bits):

```

Input bytes: [b0, b1, b2, ..., b15]

State matrix (column-major order):
┌─────┬─────┬─────┬─────┐
│ b0  │ b4  │ b8  │ b12 │
├─────┼─────┼─────┼─────┤
│ b1  │ b5  │ b9  │ b13 │
├─────┼─────┼─────┼─────┤
│ b2  │ b6  │ b10 │ b14 │
├─────┼─────┼─────┼─────┤
│ b3  │ b7  │ b11 │ b15 │
└─────┴─────┴─────┴─────┘
```

**Code chuyển đổi:**
```csharp
// Input → State
for (int i = 0; i < 4; i++)
    for (int j = 0; j < 4; j++)
        state[j, i] = input[i * 4 + j];
```

### 2.4. Key Expansion (Mở rộng khóa)

**Mục đích:** Từ khóa 256-bit ban đầu, tạo ra 15 round keys (mỗi round key 128-bit)

**Quy trình (AES.cs - KeyExpansion):**

```
AES-256: Nk = 8 words (32 bytes / 4 = 8)
Số vòng: Nr = 14
Tổng số words cần: 4 × (Nr + 1) = 60 words

Bước 1: Copy 8 words đầu từ khóa gốc
Bước 2: Tạo 52 words còn lại theo công thức:
  - Nếu i % 8 == 0:
      w[i] = w[i-8] XOR SubWord(RotWord(w[i-1])) XOR Rcon[i/8]
  - Nếu i % 8 == 4:
      w[i] = w[i-8] XOR SubWord(w[i-1])
  - Ngược lại:
      w[i] = w[i-8] XOR w[i-1]
```

**Các hàm phụ:**

1. **RotWord:** Xoay vòng 1 word (4 bytes)
   ```
   [a0, a1, a2, a3] → [a1, a2, a3, a0]
   ```

2. **SubWord:** Thay thế mỗi byte bằng S-Box
   ```
   [a0, a1, a2, a3] → [S-Box[a0], S-Box[a1], S-Box[a2], S-Box[a3]]
   ```

3. **Rcon (Round Constant):** Hằng số cho mỗi vòng
   ```
   Rcon[1] = 0x01, Rcon[2] = 0x02, Rcon[3] = 0x04, ...
   Rcon[i] = 0x02^(i-1) trong GF(2^8)
   ```

**Kết quả:** 15 round keys, mỗi key 128-bit (16 bytes)

### 2.5. S-Box (Substitution Box)

**Mục đích:** Thay thế phi tuyến, tạo confusion (làm rối dữ liệu)

**S-Box là gì?**
- Bảng tra cứu 16×16 (256 giá trị)
- Mỗi byte input (0x00 - 0xFF) được thay bằng 1 byte output
- Được tính toán dựa trên nghịch đảo trong trường Galois GF(2^8)

**Ví dụ:**
```
Input byte: 0x53
S-Box[0x53] = 0xED
```

**Code:**
```csharp
private static readonly byte[] SBox = {
    0x63, 0x7C, 0x77, 0x7B, 0xF2, 0x6B, 0x6F, 0xC5, ...
};

private void SubBytes()
{
    for (int i = 0; i < 4; i++)
        for (int j = 0; j < 4; j++)
            state[i, j] = SBox[state[i, j]];
}
```

**Inverse S-Box:** Dùng cho giải mã, là bảng nghịch đảo của S-Box
```csharp
private void InvSubBytes()
{
    for (int i = 0; i < 4; i++)
        for (int j = 0; j < 4; j++)
            state[i, j] = InvSBox[state[i, j]];
}
```

### 2.6. ShiftRows (Dịch hàng)

**Mục đích:** Tạo diffusion (khuếch tán), trộn dữ liệu giữa các cột

**Quy tắc:**

```
- Hàng 0: Không dịch
- Hàng 1: Dịch trái 1 vị trí
- Hàng 2: Dịch trái 2 vị trí
- Hàng 3: Dịch trái 3 vị trí
```

**Minh họa:**
```
Trước ShiftRows:              Sau ShiftRows:
┌────┬────┬────┬────┐        ┌────┬────┬────┬────┐
│ a0 │ a4 │ a8 │ a12│        │ a0 │ a4 │ a8 │ a12│  (hàng 0: không đổi)
├────┼────┼────┼────┤        ├────┼────┼────┼────┤
│ a1 │ a5 │ a9 │ a13│   →    │ a5 │ a9 │ a13│ a1 │  (hàng 1: dịch trái 1)
├────┼────┼────┼────┤        ├────┼────┼────┼────┤
│ a2 │ a6 │ a10│ a14│        │ a10│ a14│ a2 │ a6 │  (hàng 2: dịch trái 2)
├────┼────┼────┼────┤        ├────┼────┼────┼────┤
│ a3 │ a7 │ a11│ a15│        │ a15│ a3 │ a7 │ a11│  (hàng 3: dịch trái 3)
└────┴────┴────┴────┘        └────┴────┴────┴────┘
```

**Code:**
```csharp
private void ShiftRows()
{
    byte temp;
    
    // Hàng 1: dịch trái 1
    temp = state[1, 0];
    state[1, 0] = state[1, 1];
    state[1, 1] = state[1, 2];
    state[1, 2] = state[1, 3];
    state[1, 3] = temp;
    
    // Hàng 2: dịch trái 2
    temp = state[2, 0];
    state[2, 0] = state[2, 2];
    state[2, 2] = temp;
    temp = state[2, 1];
    state[2, 1] = state[2, 3];
    state[2, 3] = temp;
    
    // Hàng 3: dịch trái 3 (= dịch phải 1)
    temp = state[3, 3];
    state[3, 3] = state[3, 2];
    state[3, 2] = state[3, 1];
    state[3, 1] = state[3, 0];
    state[3, 0] = temp;
}
```

**InvShiftRows:** Dịch ngược lại (dịch phải)

### 2.7. MixColumns (Trộn cột)

**Mục đích:** Trộn dữ liệu trong mỗi cột, tạo diffusion mạnh mẽ

**Nguyên lý:** Nhân ma trận trong trường Galois GF(2^8)

**Ma trận nhân cố định:**
```
┌───────────┐   ┌────┐   ┌────┐
│ 2 3 1 1 │   │ s0 │   │ s0'│
│ 1 2 3 1 │ × │ s1 │ = │ s1'│
│ 1 1 2 3 │   │ s2 │   │ s2'│
│ 3 1 1 2 │   │ s3 │   │ s3'│
└───────────┘   └────┘   └────┘
```

**Công thức cho mỗi cột:**
```
s0' = (2×s0) ⊕ (3×s1) ⊕ s2 ⊕ s3
s1' = s0 ⊕ (2×s1) ⊕ (3×s2) ⊕ s3
s2' = s0 ⊕ s1 ⊕ (2×s2) ⊕ (3×s3)
s3' = (3×s0) ⊕ s1 ⊕ s2 ⊕ (2×s3)
```

**Phép nhân trong GF(2^8) - GMul:**

Phép nhân trong trường Galois khác phép nhân thông thường:
```
a × b trong GF(2^8):
1. Nhân nhị phân (như nhân đa thức)
2. Nếu kết quả > 255, XOR với 0x1B (đa thức bất khả quy)
```

**Code GMul:**
```csharp
private byte GMul(byte a, byte b)
{
    byte p = 0;
    for (int i = 0; i < 8; i++)
    {
        if ((b & 1) != 0)
            p ^= a;  // Cộng trong GF(2^8) = XOR
        
        bool hiBitSet = (a & 0x80) != 0;
        a <<= 1;  // Nhân 2
        if (hiBitSet)
            a ^= 0x1B;  // Modulo đa thức bất khả quy
        b >>= 1;
    }
    return p;
}
```

**Ví dụ:** GMul(0x57, 0x02)
```
0x57 = 01010111
Nhân 2 = dịch trái 1 bit = 10101110 = 0xAE
Bit cao = 1 → XOR 0x1B
0xAE XOR 0x1B = 10101110 XOR 00011011 = 10110101 = 0xB5
```

**InvMixColumns:** Dùng ma trận nghịch đảo
```
┌────────────────┐
│ 14 11 13  9 │
│  9 14 11 13 │
│ 13  9 14 11 │
│ 11 13  9 14 │
└────────────────┘
```

### 2.8. AddRoundKey (Cộng khóa vòng)

**Mục đích:** XOR state với round key

**Công thức đơn giản:**
```
state[i][j] = state[i][j] XOR roundKey[round][i][j]
```

**Code:**

```csharp
private void AddRoundKey(int round)
{
    for (int i = 0; i < 4; i++)
    {
        for (int j = 0; j < 4; j++)
        {
            state[j, i] ^= roundKeys[round * 4 + i, j];
        }
    }
}
```

**Tại sao dùng XOR?**
- XOR là phép toán đảo ngược: A XOR B XOR B = A
- Nhanh và hiệu quả
- Không làm mất thông tin

### 2.9. Quy trình mã hóa AES-256 (14 vòng)

**Cấu trúc tổng thể (AES.cs - EncryptBlock):**

```
Input: 16 bytes plaintext
Key: 32 bytes

Bước 0: AddRoundKey(0)  ← Khởi tạo

Vòng 1-13: (13 vòng)
    SubBytes()          ← Thay thế phi tuyến
    ShiftRows()         ← Dịch hàng
    MixColumns()        ← Trộn cột
    AddRoundKey(round)  ← Cộng khóa

Vòng 14: (vòng cuối - không có MixColumns)
    SubBytes()
    ShiftRows()
    AddRoundKey(14)

Output: 16 bytes ciphertext
```

**Code:**
```csharp
public byte[] EncryptBlock(byte[] input)
{
    // Copy input vào state (4×4 matrix)
    for (int i = 0; i < 4; i++)
        for (int j = 0; j < 4; j++)
            state[j, i] = input[i * 4 + j];

    AddRoundKey(0);  // Vòng khởi tạo

    for (int round = 1; round < Nr; round++)  // Nr = 14
    {
        SubBytes();
        ShiftRows();
        MixColumns();
        AddRoundKey(round);
    }

    // Vòng cuối
    SubBytes();
    ShiftRows();
    AddRoundKey(Nr);

    // Copy state ra output
    byte[] output = new byte[16];
    for (int i = 0; i < 4; i++)
        for (int j = 0; j < 4; j++)
            output[i * 4 + j] = state[j, i];

    return output;
}
```

**Tại sao vòng cuối không có MixColumns?**
- MixColumns không tăng thêm độ an toàn ở vòng cuối
- Bỏ đi giúp giải mã đối xứng hơn với mã hóa

### 2.10. Quy trình giải mã AES-256

**Cấu trúc (AES.cs - DecryptBlock):**

```
Input: 16 bytes ciphertext

Bước 0: AddRoundKey(14)  ← Bắt đầu từ khóa cuối

Vòng 13-1: (13 vòng ngược)
    InvShiftRows()       ← Dịch hàng ngược
    InvSubBytes()        ← Thay thế ngược
    AddRoundKey(round)   ← Cộng khóa
    InvMixColumns()      ← Trộn cột ngược

Vòng 0: (vòng cuối)
    InvShiftRows()
    InvSubBytes()
    AddRoundKey(0)

Output: 16 bytes plaintext
```

**Lưu ý:** Thứ tự các bước giải mã là ngược lại với mã hóa

### 2.11. CBC Mode (Cipher Block Chaining)

**Vấn đề với ECB mode:**
- Cùng plaintext block → cùng ciphertext block
- Lộ pattern trong dữ liệu
- Không an toàn

**Giải pháp: CBC Mode**

**Mã hóa CBC (AES.cs - EncryptCBC):**
```
C[0] = Encrypt(P[0] XOR IV)
C[1] = Encrypt(P[1] XOR C[0])
C[2] = Encrypt(P[2] XOR C[1])
...
C[i] = Encrypt(P[i] XOR C[i-1])
```

**Minh họa:**
```
IV ──┐
     ↓
P[0] ⊕ → [AES Encrypt] → C[0] ──┐
                                 ↓
                    P[1] ⊕ → [AES Encrypt] → C[1] ──┐
                                                     ↓
                                        P[2] ⊕ → [AES Encrypt] → C[2]
```

**Code:**

```csharp
public byte[] EncryptCBC(byte[] plaintext, byte[] iv)
{
    // 1. Padding PKCS7
    int paddingLength = 16 - (plaintext.Length % 16);
    byte[] paddedData = new byte[plaintext.Length + paddingLength];
    Array.Copy(plaintext, paddedData, plaintext.Length);
    for (int i = plaintext.Length; i < paddedData.Length; i++)
        paddedData[i] = (byte)paddingLength;

    byte[] ciphertext = new byte[paddedData.Length];
    byte[] previousBlock = iv;  // Block trước = IV cho block đầu

    // 2. Mã hóa từng block
    for (int i = 0; i < paddedData.Length; i += 16)
    {
        byte[] block = new byte[16];
        Array.Copy(paddedData, i, block, 0, 16);

        // XOR với block trước (hoặc IV)
        for (int j = 0; j < 16; j++)
            block[j] ^= previousBlock[j];

        // Mã hóa block
        byte[] encryptedBlock = EncryptBlock(block);
        Array.Copy(encryptedBlock, 0, ciphertext, i, 16);
        
        previousBlock = encryptedBlock;  // Cập nhật block trước
    }

    return ciphertext;
}
```

**PKCS7 Padding:**
- Nếu thiếu n bytes để đủ 16 bytes → thêm n bytes có giá trị n
- Ví dụ: "HELLO" (5 bytes) → "HELLO\x0B\x0B\x0B\x0B\x0B\x0B\x0B\x0B\x0B\x0B\x0B" (16 bytes)
- Nếu đủ 16 bytes → thêm 1 block mới toàn 0x10

**Giải mã CBC (AES.cs - DecryptCBC):**
```
P[0] = Decrypt(C[0]) XOR IV
P[1] = Decrypt(C[1]) XOR C[0]
P[2] = Decrypt(C[2]) XOR C[1]
...
P[i] = Decrypt(C[i]) XOR C[i-1]
```

**Code:**
```csharp
public byte[] DecryptCBC(byte[] ciphertext, byte[] iv)
{
    byte[] plaintext = new byte[ciphertext.Length];
    byte[] previousBlock = iv;

    for (int i = 0; i < ciphertext.Length; i += 16)
    {
        byte[] block = new byte[16];
        Array.Copy(ciphertext, i, block, 0, 16);

        // Giải mã block
        byte[] decryptedBlock = DecryptBlock(block);

        // XOR với block trước
        for (int j = 0; j < 16; j++)
            decryptedBlock[j] ^= previousBlock[j];

        Array.Copy(decryptedBlock, 0, plaintext, i, 16);
        previousBlock = block;  // Lưu ciphertext block
    }

    // Loại bỏ padding
    int paddingLength = plaintext[plaintext.Length - 1];
    byte[] result = new byte[plaintext.Length - paddingLength];
    Array.Copy(plaintext, result, result.Length);

    return result;
}
```

**Ưu điểm CBC:**
- Cùng plaintext với cùng key nhưng khác IV → khác ciphertext
- Không lộ pattern
- An toàn hơn ECB

**Nhược điểm CBC:**
- Không thể song song hóa mã hóa (phải tuần tự)
- Lỗi 1 bit trong ciphertext ảnh hưởng 2 blocks plaintext

---

## PHẦN 3: QUY TRÌNH TRUYỀN DỮ LIỆU AN TOÀN

### 3.1. Khởi tạo hệ thống

**Server:**
```csharp
// 1. Tạo cặp khóa RSA
RSAKeyPair serverKeyPair = new RSAKeyPair(1024);
// → Public Key: (n, e)
// → Private Key: (n, d)

// 2. Công khai Public Key
string publicKey = serverKeyPair.GetPublicKeyBase64();
// Gửi cho tất cả clients
```

**Client:**
```csharp
// Nhận Public Key từ server
RSAKeyPair serverPublicKey = ...; // Parse từ base64
```

### 3.2. Client mã hóa và gửi dữ liệu (ClientService.cs)

**Bước 1: Chuẩn bị dữ liệu**
```csharp
string jsonData = "{\"fullName\":\"Nguyen Van A\",\"email\":\"a@example.com\",...}";
```

**Bước 2: Tạo khóa AES ngẫu nhiên**
```csharp
AESKey aesKey = new AESKey();
// → Key: 32 bytes ngẫu nhiên
// → IV: 16 bytes ngẫu nhiên
```

**Tại sao tạo khóa mới mỗi lần?**
- Tăng độ an toàn: mỗi phiên có khóa riêng
- Nếu 1 khóa bị lộ, chỉ ảnh hưởng 1 phiên
- Forward secrecy: không thể giải mã các phiên trước

**Bước 3: Mã hóa dữ liệu bằng AES**
```csharp
AES aes = new AES(aesKey.Key);
byte[] encryptedData = aes.EncryptCBC(
    Encoding.UTF8.GetBytes(jsonData), 
    aesKey.IV
);
```

**Quy trình:**
```
jsonData (plaintext)
    ↓ UTF8 encoding
byte[] plainBytes
    ↓ AES-256-CBC (với Key và IV)
byte[] encryptedData
```

**Bước 4: Mã hóa khóa AES bằng RSA**
```csharp
RSA rsa = new RSA(serverPublicKey.N, serverPublicKey.E);
byte[] encryptedKey = rsa.Encrypt(aesKey.Key);
```

**Quy trình:**
```
aesKey.Key (32 bytes)
    ↓ RSA Encrypt với Public Key
byte[] encryptedKey (128 bytes với RSA-1024)
```

**Bước 5: Tạo packet truyền**

```csharp
TransmissionPacket packet = new TransmissionPacket
{
    EncryptedData = Convert.ToBase64String(encryptedData),
    EncryptedAESKey = Convert.ToBase64String(encryptedKey),
    IV = Convert.ToBase64String(aesKey.IV)
};
```

**Cấu trúc packet:**
```
┌─────────────────────────────────────────┐
│ EncryptedData (Base64)                  │ ← Dữ liệu đã mã hóa AES
│ "xK8j3mP9..." (có thể rất dài)          │
├─────────────────────────────────────────┤
│ EncryptedAESKey (Base64)                │ ← Khóa AES đã mã hóa RSA
│ "mN7pQ2..." (~172 chars cho RSA-1024)   │
├─────────────────────────────────────────┤
│ IV (Base64)                             │ ← IV cho AES (không mã hóa)
│ "aB3dE5..." (24 chars)                  │
└─────────────────────────────────────────┘
```

**Bước 6: Gửi packet qua mạng**
```
Client → [Internet - Kênh công khai] → Server
```

**Dữ liệu trên kênh truyền:**
- Tất cả đều là ciphertext (trừ IV)
- Kẻ tấn công có thể thấy nhưng không giải mã được
- Cần cả Private Key (để giải mã AES key) và AES key (để giải mã data)

### 3.3. Server nhận và giải mã (ServerService.cs)

**Bước 1: Nhận packet**
```csharp
TransmissionPacket packet = ...; // Nhận từ client
```

**Bước 2: Giải mã khóa AES bằng RSA Private Key**
```csharp
RSA rsa = new RSA(serverKeyPair.N, serverKeyPair.E, serverKeyPair.D);
byte[] aesKeyBytes = rsa.Decrypt(
    Convert.FromBase64String(packet.EncryptedAESKey)
);
```

**Quy trình:**
```
EncryptedAESKey (Base64)
    ↓ Base64 decode
byte[] encryptedKey
    ↓ RSA Decrypt với Private Key
byte[] aesKey (32 bytes)
```

**Chỉ server có Private Key → chỉ server giải mã được!**

**Bước 3: Giải mã dữ liệu bằng AES**
```csharp
AES aes = new AES(aesKeyBytes);
byte[] decryptedData = aes.DecryptCBC(
    Convert.FromBase64String(packet.EncryptedData),
    Convert.FromBase64String(packet.IV)
);
```

**Quy trình:**
```
EncryptedData (Base64)
    ↓ Base64 decode
byte[] encryptedData
    ↓ AES-256-CBC Decrypt (với aesKey và IV)
byte[] plainBytes
    ↓ UTF8 decode
string jsonData (plaintext)
```

**Bước 4: Parse dữ liệu**
```csharp
string jsonData = Encoding.UTF8.GetString(decryptedData);
// Parse JSON để lấy fullName, email, phone, ...
```

### 3.4. Sơ đồ tổng thể

```
┌─────────────────────────────────────────────────────────────────────┐
│                            CLIENT                                    │
├─────────────────────────────────────────────────────────────────────┤
│ 1. Dữ liệu gốc: "Nguyen Van A", "a@example.com", ...               │
│    ↓                                                                 │
│ 2. Tạo JSON: {"fullName":"Nguyen Van A",...}                        │
│    ↓                                                                 │
│ 3. Tạo AES Key ngẫu nhiên (32 bytes) + IV (16 bytes)               │
│    ↓                                                                 │
│ 4. Mã hóa JSON bằng AES-256-CBC                                     │
│    → EncryptedData                                                   │
│    ↓                                                                 │
│ 5. Mã hóa AES Key bằng RSA Public Key                              │
│    → EncryptedAESKey                                                 │
│    ↓                                                                 │
│ 6. Tạo packet: {EncryptedData, EncryptedAESKey, IV}                │
└─────────────────────────────────────────────────────────────────────┘
                              ↓
                    [INTERNET - Kênh công khai]
                    Kẻ tấn công chỉ thấy ciphertext
                              ↓
┌─────────────────────────────────────────────────────────────────────┐
│                            SERVER                                    │
├─────────────────────────────────────────────────────────────────────┤
│ 1. Nhận packet: {EncryptedData, EncryptedAESKey, IV}               │
│    ↓                                                                 │
│ 2. Giải mã EncryptedAESKey bằng RSA Private Key                    │
│    → AES Key (32 bytes)                                             │
│    ↓                                                                 │
│ 3. Giải mã EncryptedData bằng AES Key + IV                         │
│    → JSON plaintext                                                  │
│    ↓                                                                 │
│ 4. Parse JSON để lấy dữ liệu gốc                                   │
│    → "Nguyen Van A", "a@example.com", ...                           │
└─────────────────────────────────────────────────────────────────────┘
```

---

## PHẦN 4: BẢO MẬT VÀ PHÂN TÍCH

### 4.1. Tại sao kết hợp RSA + AES?

**Vấn đề nếu chỉ dùng RSA:**
- RSA chậm (phép toán số lớn)
- Giới hạn kích thước dữ liệu
- Không phù hợp cho dữ liệu lớn

**Vấn đề nếu chỉ dùng AES:**
- Làm sao trao đổi khóa an toàn?
- Nếu gửi khóa qua mạng → kẻ tấn công có thể chặn

**Giải pháp Hybrid (RSA + AES):**
- AES mã hóa dữ liệu (nhanh, không giới hạn kích thước)
- RSA mã hóa khóa AES (an toàn, chỉ mã hóa 32 bytes)
- Kết hợp ưu điểm của cả hai

### 4.2. Các lớp bảo mật

**Lớp 1: Mã hóa dữ liệu (AES-256)**
- Dữ liệu được mã hóa bằng AES-256 (256-bit key)
- CBC mode với IV ngẫu nhiên
- Kẻ tấn công không thể đọc được dữ liệu

**Lớp 2: Bảo vệ khóa AES (RSA-1024)**
- Khóa AES được mã hóa bằng RSA
- Chỉ server có Private Key mới giải mã được
- Kẻ tấn công không lấy được khóa AES

**Lớp 3: Khóa phiên (Session Key)**
- Mỗi lần truyền tạo khóa AES mới
- Forward secrecy: lộ 1 khóa không ảnh hưởng các phiên khác

### 4.3. Các cuộc tấn công và phòng thủ

**1. Man-in-the-Middle (MITM)**

- **Tấn công:** Kẻ tấn công chặn và thay đổi Public Key
- **Phòng thủ:** Cần xác thực Public Key (certificate, PKI)
- **Trong code:** Chưa có xác thực (giả định kênh an toàn cho trao đổi Public Key)

**2. Brute Force Attack**
- **Tấn công:** Thử tất cả khóa có thể
- **Phòng thủ:** 
  - AES-256: 2^256 khả năng (không khả thi)
  - RSA-1024: Cần phân tích n thành p×q (rất khó)

**3. Known Plaintext Attack**
- **Tấn công:** Biết cả plaintext và ciphertext, tìm khóa
- **Phòng thủ:** AES và RSA đều kháng được tấn công này

**4. Replay Attack**
- **Tấn công:** Gửi lại packet cũ
- **Phòng thủ:** Cần thêm timestamp hoặc nonce (chưa có trong code)

**5. Padding Oracle Attack**
- **Tấn công:** Dựa vào lỗi padding để giải mã
- **Phòng thủ:** Không tiết lộ thông tin lỗi padding

### 4.4. Độ mạnh mã hóa

**AES-256:**
- Khóa: 256 bit = 2^256 ≈ 10^77 khả năng
- Thời gian brute force: Hàng tỷ tỷ năm với siêu máy tính
- Được NSA chấp nhận cho thông tin tối mật

**RSA-1024:**
- Modulus: 1024 bit
- Độ an toàn tương đương: ~80-bit symmetric key
- Khuyến nghị hiện tại: RSA-2048 trở lên
- RSA-1024 vẫn an toàn cho hầu hết ứng dụng

### 4.5. Hiệu năng

**Thời gian ước tính (phụ thuộc phần cứng):**

```
Tạo khóa RSA-1024: ~100-500ms
Mã hóa RSA (32 bytes): ~1-5ms
Giải mã RSA (128 bytes): ~5-20ms

Tạo khóa AES: <1ms (random)
Mã hóa AES (1MB): ~10-50ms
Giải mã AES (1MB): ~10-50ms
```

**So sánh:**
- AES nhanh hơn RSA ~100-1000 lần
- RSA chỉ dùng cho dữ liệu nhỏ (khóa)
- AES xử lý dữ liệu lớn

---

## PHẦN 5: CHI TIẾT KỸ THUẬT BỔ SUNG

### 5.1. Tại sao dùng BigInteger?

**Vấn đề:**
- RSA cần tính toán với số rất lớn (1024 bit = 309 chữ số thập phân)
- Kiểu dữ liệu thông thường (int, long) không đủ

**Giải pháp:**
- C# cung cấp `System.Numerics.BigInteger`
- Hỗ trợ số nguyên tùy ý độ dài
- Các phép toán: +, -, ×, /, %, ^

### 5.2. Tối ưu hóa ModPow

**Vấn đề:** Tính a^b mod m với b rất lớn (hàng trăm bit)

**Phương pháp Square-and-Multiply:**
```
Ví dụ: 5^13 mod 7
13 = 1101₂ (nhị phân)

Khởi tạo: result = 1, base = 5

Bit 1 (LSB): result = 1×5 = 5, base = 5² = 25 mod 7 = 4
Bit 0:       base = 4² = 16 mod 7 = 2
Bit 1:       result = 5×2 = 10 mod 7 = 3, base = 2² = 4
Bit 1:       result = 3×4 = 12 mod 7 = 5

Kết quả: 5
```

**Độ phức tạp:**
- Naive: O(b) phép nhân
- Square-and-Multiply: O(log b) phép nhân
- Với b = 2^256: giảm từ 10^77 xuống 256 phép nhân!

### 5.3. Galois Field GF(2^8)

**Tại sao AES dùng GF(2^8)?**
- Mỗi byte (8 bit) là 1 phần tử trong trường
- Phép toán đóng (kết quả luôn là byte)
- Có tính chất đại số tốt (mỗi phần tử khác 0 có nghịch đảo)

**Phép cộng trong GF(2^8):**
```
a + b = a XOR b
Ví dụ: 0x57 + 0x83 = 01010111 XOR 10000011 = 11010100 = 0xD4
```

**Phép nhân trong GF(2^8):**
```
a × b = (a × b) mod P(x)
P(x) = x^8 + x^4 + x^3 + x + 1 = 0x11B (đa thức bất khả quy)

Ví dụ: 0x57 × 0x02
= 01010111 << 1 = 10101110
Bit cao = 1 → XOR 0x1B
= 10101110 XOR 00011011 = 10110101 = 0xB5
```

### 5.4. S-Box Construction

**Cách tạo S-Box:**
1. Tính nghịch đảo của x trong GF(2^8): y = x^(-1)
2. Áp dụng biến đổi affine:
   ```
   b[i] = b[i] XOR b[(i+4)%8] XOR b[(i+5)%8] XOR b[(i+6)%8] XOR b[(i+7)%8] XOR c[i]
   ```
   với c = 0x63

**Ví dụ:** S-Box[0x53]
```
1. 0x53^(-1) trong GF(2^8) = 0xCA
2. Áp dụng affine transform lên 0xCA
3. Kết quả: 0xED
```

**Tính chất S-Box:**
- Phi tuyến (non-linear)
- Không có điểm bất động (S[x] ≠ x)
- Kháng differential và linear cryptanalysis

### 5.5. MixColumns Matrix

**Tại sao chọn ma trận này?**
```
┌───────────┐
│ 2 3 1 1 │
│ 1 2 3 1 │
│ 1 1 2 3 │
│ 3 1 1 2 │
└───────────┘
```

**Tính chất:**
- Ma trận MDS (Maximum Distance Separable)
- Thay đổi 1 byte input → thay đổi tất cả 4 bytes output
- Có ma trận nghịch đảo trong GF(2^8)
- Tối ưu cho diffusion

**Branch number = 5:**
- Số byte khác 0 ở input + output ≥ 5
- Đảm bảo diffusion tốt

### 5.6. Key Schedule Security

**Tại sao cần Key Expansion phức tạp?**
- Tạo 15 round keys khác nhau từ 1 key
- Mỗi round key phải "độc lập"
- Ngăn chặn related-key attacks

**Các thành phần:**
- **RotWord:** Tạo sự phụ thuộc giữa các byte
- **SubWord:** Thêm tính phi tuyến
- **Rcon:** Đảm bảo mỗi round khác nhau

---

## PHẦN 6: VÍ DỤ CỤ THỂ

### 6.1. Ví dụ RSA đơn giản (số nhỏ)

**Tạo khóa:**
```
Chọn p = 61, q = 53
n = p × q = 61 × 53 = 3233
φ(n) = (61-1) × (53-1) = 60 × 52 = 3120

Chọn e = 17 (GCD(17, 3120) = 1)
Tính d: 17 × d ≡ 1 (mod 3120)
→ d = 2753

Public Key: (3233, 17)
Private Key: (3233, 2753)
```

**Mã hóa:**
```
Message: M = 123
Ciphertext: C = 123^17 mod 3233

Tính từng bước:
123^1 = 123
123^2 = 15129 mod 3233 = 2430
123^4 = 2430^2 = 5904900 mod 3233 = 2288
123^8 = 2288^2 = 5234944 mod 3233 = 822
123^16 = 822^2 = 675684 mod 3233 = 2790

17 = 16 + 1 (nhị phân: 10001)
C = 123^17 = 123^16 × 123^1 = 2790 × 123 = 343170 mod 3233 = 855
```

**Giải mã:**
```
Ciphertext: C = 855
Message: M = 855^2753 mod 3233
(Tính tương tự với ModPow)
→ M = 123 ✓
```

### 6.2. Ví dụ AES đơn giản (1 round)

**Input:**
```
Plaintext: "HELLO WORLD 1234" (16 bytes)
Key: "YELLOW SUBMARINE" (16 bytes - AES-128 cho đơn giản)

State matrix:
┌────┬────┬────┬────┐
│ H  │ O  │ O  │ 1  │
│ E  │    │ R  │ 2  │
│ L  │ W  │ L  │ 3  │
│ L  │ O  │ D  │ 4  │
└────┴────┴────┴────┘
```

**Sau SubBytes:**
```
H → S-Box[0x48] = 0x52
E → S-Box[0x45] = 0x7C
...
```

**Sau ShiftRows:**
```
┌────┬────┬────┬────┐
│ 52 │ 7C │ 77 │ 7B │  (hàng 0: không đổi)
│ 6B │ 6F │ C5 │ 30 │  (hàng 1: dịch trái 1)
│ A2 │ AF │ 9C │ A4 │  (hàng 2: dịch trái 2)
│ C0 │ B7 │ FD │ 93 │  (hàng 3: dịch trái 3)
└────┴────┴────┴────┘
```

**Sau MixColumns:**
```
Cột 0: [52, 6B, A2, C0]
s0' = (2×52) ⊕ (3×6B) ⊕ A2 ⊕ C0
    = A4 ⊕ 41 ⊕ A2 ⊕ C0 = ...
(Tính tương tự cho các cột khác)
```

**Sau AddRoundKey:**
```
State XOR RoundKey[1]
```

### 6.3. Ví dụ CBC Mode

**Plaintext:** "HELLO" (5 bytes)

**Bước 1: Padding PKCS7**
```
"HELLO" → "HELLO\x0B\x0B\x0B\x0B\x0B\x0B\x0B\x0B\x0B\x0B\x0B"
(Thêm 11 bytes có giá trị 0x0B)
```

**Bước 2: Mã hóa**
```
IV = [random 16 bytes]
P[0] = "HELLO\x0B\x0B..." (16 bytes)

Block 0:
  Input = P[0] XOR IV
  C[0] = AES_Encrypt(Input)
```

**Bước 3: Truyền**
```
Gửi: C[0] + IV (32 bytes)
```

**Bước 4: Giải mã**
```
Nhận: C[0] + IV
Temp = AES_Decrypt(C[0])
P[0] = Temp XOR IV
Loại bỏ padding → "HELLO"
```

---

## PHẦN 7: KẾT LUẬN

### 7.1. Tóm tắt hệ thống

Hệ thống sử dụng **Hybrid Encryption** kết hợp RSA và AES:

1. **RSA-1024** cho trao đổi khóa:
   - Tạo cặp khóa từ 2 số nguyên tố lớn
   - Public Key mã hóa, Private Key giải mã
   - Chỉ mã hóa khóa AES (32 bytes)

2. **AES-256-CBC** cho mã hóa dữ liệu:
   - Khóa 256-bit, IV 128-bit
   - 14 vòng biến đổi (SubBytes, ShiftRows, MixColumns, AddRoundKey)
   - CBC mode với PKCS7 padding

3. **Quy trình truyền:**
   - Client: Tạo AES key → Mã hóa data → Mã hóa AES key → Gửi
   - Server: Giải mã AES key → Giải mã data

### 7.2. Ưu điểm

- **An toàn:** Kết hợp 2 thuật toán mạnh
- **Hiệu quả:** AES nhanh cho dữ liệu lớn
- **Linh hoạt:** Mỗi phiên có khóa riêng
- **Đơn giản:** Dễ triển khai và bảo trì

### 7.3. Hạn chế và cải tiến

**Hạn chế hiện tại:**
- RSA-1024 nên nâng lên RSA-2048
- Chưa có xác thực Public Key
- Chưa có chống replay attack
- Chưa có integrity check (HMAC)

**Cải tiến đề xuất:**
1. Nâng cấp RSA-2048 hoặc RSA-4096
2. Thêm certificate/PKI cho Public Key
3. Thêm timestamp/nonce chống replay
4. Thêm HMAC-SHA256 cho integrity
5. Xem xét dùng AES-GCM thay CBC
6. Implement Perfect Forward Secrecy (PFS)

### 7.4. Ứng dụng thực tế

Hệ thống tương tự được dùng trong:
- **HTTPS/TLS:** Bảo mật web (RSA/ECDHE + AES-GCM)
- **SSH:** Kết nối remote an toàn
- **VPN:** Mã hóa kết nối mạng
- **Email encryption:** PGP, S/MIME
- **Messaging apps:** Signal, WhatsApp (với ECDH)

---

## PHỤ LỤC: BẢNG TRA CỨU NHANH

### Các tham số chính

| Thành phần | Kích thước | Mô tả |
|------------|-----------|-------|
| RSA Modulus (n) | 1024 bit | 128 bytes |
| RSA Public Exponent (e) | 17 bit | Thường là 65537 |
| RSA Private Exponent (d) | ~1024 bit | Bí mật |
| AES Key | 256 bit | 32 bytes |
| AES IV | 128 bit | 16 bytes |
| AES Block | 128 bit | 16 bytes (4×4 matrix) |
| AES Rounds | 14 | Cho AES-256 |

### Độ phức tạp thuật toán

| Thuật toán | Mã hóa | Giải mã | Tạo khóa |
|------------|--------|---------|----------|
| RSA | O(log e) | O(log d) | O(k³) |
| AES | O(n) | O(n) | O(1) |
| Miller-Rabin | - | - | O(k² log n) |

(n: kích thước dữ liệu, k: kích thước khóa)

### Các hằng số quan trọng

```
RSA e (thường dùng): 65537 = 0x10001
AES S-Box[0]: 0x63
AES Rcon[1]: 0x01
AES Polynomial: 0x11B (x^8 + x^4 + x^3 + x + 1)
PKCS7 Padding: Thêm n bytes có giá trị n
```

---

**Tài liệu này mô tả chi tiết cách triển khai hệ thống mã hóa hybrid RSA + AES trong dự án DataMasking.**

**Phiên bản:** 1.0  
**Ngày tạo:** 2026-03-06
