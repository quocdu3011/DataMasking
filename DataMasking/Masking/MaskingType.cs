namespace DataMasking.Masking
{
    public enum MaskingType
    {
        CharacterMask,   // Che mặt nạ ký tự (mặc định)
        Shuffle,         // Xáo trộn dữ liệu
        FakeData,        // Thay thế bằng dữ liệu giả
        NumericNoise     // Thêm nhiễu vào ký tự số
    }
}
