using System.Collections.Generic;

namespace DataMasking.Models
{
    public class ActvnLoginRequest
    {
        public string Action { get; set; } = "actvn_login";
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class ActvnScheduleRequest
    {
        public string Action { get; set; } = "actvn_schedule";
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class ActvnLoginResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public ActvnStudentInfo StudentInfo { get; set; }
    }

    public class ActvnScheduleResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public ActvnStudentInfo StudentInfo { get; set; }
        public List<ActvnCalendarEvent> Events { get; set; }
    }

    public class ActvnStudentInfo
    {
        public string StudentCode { get; set; }
        public string StudentName { get; set; }
        public string Gender { get; set; }
        public string Birthday { get; set; }
        public string BankAccount { get; set; }
        public string IdCard { get; set; }
        public string BirthPlace { get; set; }
        public string PersonalPhone { get; set; }
        public string Email { get; set; }
        public string EmergencyContact { get; set; }
    }

    public class ActvnProfileRequest
    {
        public string Action { get; set; } = "actvn_profile";
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class ActvnProfileResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public ActvnStudentInfo StudentInfo { get; set; }
    }

    public class ActvnCalendarEvent
    {
        public string Title { get; set; }
        public string CourseCode { get; set; }
        public string Teacher { get; set; }
        public string Location { get; set; }
        public string Date { get; set; }
        public List<int> Lessons { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public int DayOfWeek { get; set; }
    }
}
