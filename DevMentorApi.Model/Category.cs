﻿namespace DevMentorApi.Model
{
    public class Category
    {
        public CategoryGroup Group { get; set; }

        public string Name { get; set; }

        public bool Reviewed { get; set; }

        public bool Visible { get; set; }
    }
}