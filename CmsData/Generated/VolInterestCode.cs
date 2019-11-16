using System;
using System.ComponentModel;
using System.Data.Linq;
using System.Data.Linq.Mapping;

namespace CmsData
{
    [Table(Name = "dbo.VolInterestCodes")]
    public partial class VolInterestCode : INotifyPropertyChanging, INotifyPropertyChanged
    {
        private static PropertyChangingEventArgs emptyChangingEventArgs = new PropertyChangingEventArgs("");

        #region Private Fields

        private int _Id;

        private string _Description;

        private string _Code;

        private string _Org;

        private EntitySet<VolInterestInterestCode> _VolInterestInterestCodes;

        #endregion

        #region Extensibility Method Definitions

        partial void OnLoaded();
        partial void OnValidate(System.Data.Linq.ChangeAction action);
        partial void OnCreated();

        partial void OnIdChanging(int value);
        partial void OnIdChanged();

        partial void OnDescriptionChanging(string value);
        partial void OnDescriptionChanged();

        partial void OnCodeChanging(string value);
        partial void OnCodeChanged();

        partial void OnOrgChanging(string value);
        partial void OnOrgChanged();

        #endregion

        public VolInterestCode()
        {
            _VolInterestInterestCodes = new EntitySet<VolInterestInterestCode>(new Action<VolInterestInterestCode>(attach_VolInterestInterestCodes), new Action<VolInterestInterestCode>(detach_VolInterestInterestCodes));

            OnCreated();
        }

        #region Columns

        [Column(Name = "Id", UpdateCheck = UpdateCheck.Never, Storage = "_Id", AutoSync = AutoSync.OnInsert, DbType = "int NOT NULL IDENTITY", IsPrimaryKey = true, IsDbGenerated = true)]
        public int Id
        {
            get => _Id;

            set
            {
                if (_Id != value)
                {
                    OnIdChanging(value);
                    SendPropertyChanging();
                    _Id = value;
                    SendPropertyChanged("Id");
                    OnIdChanged();
                }
            }
        }

        [Column(Name = "Description", UpdateCheck = UpdateCheck.Never, Storage = "_Description", DbType = "nvarchar(180)")]
        public string Description
        {
            get => _Description;

            set
            {
                if (_Description != value)
                {
                    OnDescriptionChanging(value);
                    SendPropertyChanging();
                    _Description = value;
                    SendPropertyChanged("Description");
                    OnDescriptionChanged();
                }
            }
        }

        [Column(Name = "Code", UpdateCheck = UpdateCheck.Never, Storage = "_Code", DbType = "nvarchar(100)")]
        public string Code
        {
            get => _Code;

            set
            {
                if (_Code != value)
                {
                    OnCodeChanging(value);
                    SendPropertyChanging();
                    _Code = value;
                    SendPropertyChanged("Code");
                    OnCodeChanged();
                }
            }
        }

        [Column(Name = "Org", UpdateCheck = UpdateCheck.Never, Storage = "_Org", DbType = "nvarchar(150)")]
        public string Org
        {
            get => _Org;

            set
            {
                if (_Org != value)
                {
                    OnOrgChanging(value);
                    SendPropertyChanging();
                    _Org = value;
                    SendPropertyChanged("Org");
                    OnOrgChanged();
                }
            }
        }

        #endregion

        #region Foreign Key Tables

        [Association(Name = "FK_VolInterestInterestCodes_VolInterestCodes", Storage = "_VolInterestInterestCodes", OtherKey = "InterestCodeId")]
        public EntitySet<VolInterestInterestCode> VolInterestInterestCodes
           {
               get => _VolInterestInterestCodes;

            set => _VolInterestInterestCodes.Assign(value);

           }

        #endregion

        #region Foreign Keys

        #endregion

        public event PropertyChangingEventHandler PropertyChanging;
        protected virtual void SendPropertyChanging()
        {
            if ((PropertyChanging != null))
            {
                PropertyChanging(this, emptyChangingEventArgs);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void SendPropertyChanged(string propertyName)
        {
            if ((PropertyChanged != null))
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private void attach_VolInterestInterestCodes(VolInterestInterestCode entity)
        {
            SendPropertyChanging();
            entity.VolInterestCode = this;
        }

        private void detach_VolInterestInterestCodes(VolInterestInterestCode entity)
        {
            SendPropertyChanging();
            entity.VolInterestCode = null;
        }
    }
}
