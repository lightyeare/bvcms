﻿using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Web.Mvc;
using CmsData;
using CmsData.Codes;
using CmsData.Classes.Barcodes;
using CmsWeb.Areas.Public.Models.CheckInAPIv2;
using CmsWeb.Areas.Public.Models.CheckInAPIv2.Results;
using CmsWeb.Areas.Public.Models.CheckInAPIv2.Searches;
using CmsWeb.Lifecycle;
using CmsWeb.Models;
using ImageData;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UtilityExtensions;
using Country = CmsWeb.Areas.Public.Models.CheckInAPIv2.Country;
using Family = CmsWeb.Areas.Public.Models.CheckInAPIv2.Family;
using Gender = CmsWeb.Areas.Public.Models.CheckInAPIv2.Gender;
using MaritalStatus = CmsWeb.Areas.Public.Models.CheckInAPIv2.MaritalStatus;

namespace CmsWeb.Areas.Public.Controllers
{
	public class CheckInAPIv2Controller : CMSBaseController
	{
		public CheckInAPIv2Controller( IRequestManager requestManager ) : base( requestManager ) { }

		public ActionResult Exists()
		{
			return Content( "1" );
		}

		private static bool Auth()
		{
			CMSDataContext db = CMSDataContext.Create( HttpContextFactory.Current );
			CMSImageDataContext idb = CMSImageDataContext.Create( HttpContextFactory.Current );

			return AccountModel.AuthenticateMobile( db, idb, "Checkin" ).IsValid;
		}

		public ActionResult Authenticate( string data )
		{
			if( !Auth() ) {
				return Message.createErrorReturn( "Authentication failed, please try again", Message.API_ERROR_INVALID_CREDENTIALS );
			}

			List<SettingsEntry> settings = (from s in CurrentDatabase.CheckInSettings
														where s.Version == 2
														select new SettingsEntry {
															id = s.Id,
															name = s.Name,
															settings = s.Settings,
															version = s.Version
														}).ToList();

			List<State> states = (from s in CurrentDatabase.StateLookups
										orderby s.StateName
										select new State {
											code = s.StateCode,
											name = s.StateName
										}).ToList();

			List<Country> countries = (from c in CurrentDatabase.Countries
												orderby c.Id
												select new Country {
													id = c.Id,
													code = c.Code,
													name = c.Description
												}).ToList();

			List<Campus> campuses = (from c in CurrentDatabase.Campus
											where c.Organizations.Any( o => o.CanSelfCheckin.Value )
											orderby c.Id
											select new Campus {
												id = c.Id,
												name = c.Description
											}).ToList();

			campuses.Insert( 0, new Campus {
				id = 0,
				name = "All Campuses"
			} );

			List<Gender> genders = (from g in CurrentDatabase.Genders
											orderby g.Id
											select new Gender {
												id = g.Id,
												name = g.Description
											}).ToList();

			List<MaritalStatus> maritalStatuses = (from m in CurrentDatabase.MaritalStatuses
																orderby m.Id
																select new MaritalStatus {
																	id = m.Id,
																	code = m.Code,
																	name = m.Description
																}).ToList();

			Information information = new Information {
				userID = Util.UserId,
				userName = Util.UserFullName,
				settings = settings,
				states = states,
				countries = countries,
				campuses = campuses,
				genders = genders,
				maritals = maritalStatuses
			};

			Message response = new Message();
			response.setNoError();
			response.data = JsonConvert.SerializeObject( information );

			return response;
		}

		[HttpPost]
		public ActionResult Search( string data )
		{
			if( !Auth() ) {
				return Message.createErrorReturn( "Authentication failed, please try again", Message.API_ERROR_INVALID_CREDENTIALS );
			}

			Message message = Message.createFromString( data );
			NumberSearch cns = JsonConvert.DeserializeObject<NumberSearch>( message.data );

			DbUtil.LogActivity( "Check-In Search: " + cns.search );
            
            Message response = new Message();
            response.setNoError();

            bool returnPictureUrls = message.device == Message.API_DEVICE_WEB;
            Guid guid;

            // handle scanned qr code
            if (Guid.TryParse(cns.search, out guid))
            {
                try
                {
                    var person = CmsData.Person.PersonForQRCode(CurrentDatabase, guid);
                    // first try to find a pending check in for the person scanned
                    var pending = CurrentDatabase.CheckInPendings.Where(p => p.PeopleId == person.PeopleId).SingleOrDefault();
                    if (pending == null)
                    {
                        // if not, see if there's a pending check in for the family
                        pending = CurrentDatabase.CheckInPendings.Where(p => p.FamilyId == person.FamilyId).SingleOrDefault();
                    }
                    if (pending != null)
                    {
                        // found a pending check in, load that data
                        AttendanceBundle bundle = JsonConvert.DeserializeObject<AttendanceBundle>(pending.Data);
                        List<Family> result = Family.forAttendanceBundle(CurrentDatabase, CurrentImageDatabase, bundle, cns.campus, cns.date, returnPictureUrls);
                        response.argString = SerializeJSON(bundle, message.version);
                        response.data = SerializeJSON(result, message.version);
                        return response;
                    }
                    else
                    {
                        // a qr code was scanned without any pending check in, just load the family without attendance data
                        List<Family> scanned = new List<Family>();
                        var family = Family.forID(CurrentDatabase, CurrentImageDatabase, person.FamilyId, cns.campus, cns.date, returnPictureUrls);
                        scanned.Add(family);
                        response.data = SerializeJSON(scanned, message.version);
                        return response;
                    }
                }
                catch (Exception e)
                {
                    return Message.createErrorReturn(e.Message, Message.API_ERROR_PERSON_NOT_FOUND);
                }
            }

            List<Family> families = Family.forSearch(CurrentDatabase, CurrentImageDatabase, cns.search, cns.campus, cns.date, returnPictureUrls);
            response.data = SerializeJSON(families, message.version);
            
			return response;
		}
        
		[HttpGet]
		public ActionResult GetProfiles()
        {
            if (CurrentDatabase.CheckinProfiles.Count() == 0)
            {
                CheckinProfilesModel.CreateDefault(CurrentDatabase);
            }

            List<Profile> profiles = new List<Profile>();
			List<CheckinProfileSetting> profileSettings = CurrentDatabase.CheckinProfileSettings.ToList();

			foreach( CheckinProfileSetting settings in profileSettings ) {
				Profile profile = new Profile(CurrentDatabase);
				profile.populate( settings );

				profiles.Add( profile );
			}

			return Json( profiles, JsonRequestBehavior.AllowGet );
		}
        
		[HttpPost]
		public ActionResult GetPerson( string data )
		{
			// Authenticate first
			if( !Auth() ) {
				return Message.createErrorReturn( "Authentication failed, please try again", Message.API_ERROR_INVALID_CREDENTIALS );
			}

			Message message = Message.createFromString( data );

			CmsData.Person p = CurrentDatabase.LoadPersonById( message.id );

			if( p == null ) {
				return Message.createErrorReturn( "Person not found", Message.API_ERROR_PERSON_NOT_FOUND );
			}

			Models.CheckInAPIv2.Person person = new Models.CheckInAPIv2.Person();
			person.populate( p );

			Message response = new Message();
			response.setNoError();
			response.count = 1;
			response.data = SerializeJSON( person );

			return response;
		}

        [HttpPost]
        public ActionResult GetQRCodeForPerson(string data)
        {
            // Authenticate first
            if (!Auth())
            {
                return Message.createErrorReturn("Authentication failed, please try again", Message.API_ERROR_INVALID_CREDENTIALS);
            }

            Message message = Message.createFromString(data);
            
            int size = (message.argInt == 0) ? 300 : message.argInt;
            string QRCode;

            try
            {
                QRCode = CmsData.Person.QRCode(CurrentDatabase, message.id, size);
            }
            catch (Exception e)
            {
                return Message.createErrorReturn(e.Message, Message.API_ERROR_PERSON_NOT_FOUND);
            }
            
            Message response = new Message();
            response.setNoError();
            response.count = 1;
            response.data = QRCode;

            return response;
        }

        [HttpPost]
		public ActionResult AddPerson( string data )
		{
			if( !Auth() ) {
				return Message.createErrorReturn( "Authentication failed, please try again", Message.API_ERROR_INVALID_CREDENTIALS );
			}

			Message message = Message.createFromString( data );
			Models.CheckInAPIv2.Person person = JsonConvert.DeserializeObject<Models.CheckInAPIv2.Person>( message.data );
			person.clean();

			// Create or Edit Family
			CmsData.Family f = person.familyID > 0 ? CurrentDatabase.Families.First( fam => fam.FamilyId == person.familyID ) : new CmsData.Family();
			person.fillFamily( f );

			if( person.familyID == 0 ) {
				CurrentDatabase.Families.InsertOnSubmit( f );
			}

			// Create Person
			CmsData.Person p = new CmsData.Person {
				CreatedDate = Util.Now,
				CreatedBy = Util.UserId,
				MemberStatusId = MemberStatusCode.JustAdded,
				AddressTypeId = 10,
				OriginId = OriginCode.Visit,
				EntryPoint = getCheckInEntryPointID(),
				CampusId = person.campus > 0 ? person.campus : (int?) null,
				Name = ""
			};

			person.fillPerson( p );

			// Calculate position before submitting changes so they aren't part of the calculation
			using( SqlConnection db = new SqlConnection( Util.ConnectionString ) ) {
				p.PositionInFamilyId = person.computePositionInFamily( db );
			}

			// p.PositionInFamilyId = CurrentDatabase.ComputePositionInFamily( person.getAge(),
			// 																						person.maritalStatusID == CmsData.Codes.MaritalStatusCode.Married, f.FamilyId ) ?? CmsData.Codes.PositionInFamily.PrimaryAdult;

			f.People.Add( p );

			CurrentDatabase.SubmitChanges();

			AddEditPersonResults results = new AddEditPersonResults {
				familyID = f.FamilyId,
				peopleID = p.PeopleId,
				positionID = p.PositionInFamilyId,
                barcodeID = CmsData.Person.Barcode(CurrentDatabase, p.PeopleId)
            };

			Message response = new Message();
			response.setNoError();
			response.count = 1;
			response.data = SerializeJSON( results );

			return response;
		}

		[HttpPost]
		public ActionResult EditPerson( string data )
		{
			// Authenticate first
			if( !Auth() ) {
				return Message.createErrorReturn( "Authentication failed, please try again", Message.API_ERROR_INVALID_CREDENTIALS );
			}

			Message message = Message.createFromString( data );
			Models.CheckInAPIv2.Person person = JsonConvert.DeserializeObject<Models.CheckInAPIv2.Person>( message.data );
			person.clean();

			CmsData.Person p = CurrentDatabase.LoadPersonById( person.id );

			if( p == null ) {
				return Message.createErrorReturn( "Person not found", Message.API_ERROR_PERSON_NOT_FOUND );
			}

			CmsData.Family f = CurrentDatabase.Families.First( fam => fam.FamilyId == p.FamilyId );

			person.fillPerson( p );
			person.fillFamily( f );

			CurrentDatabase.SubmitChanges();

            AddEditPersonResults results = new AddEditPersonResults {
                familyID = f.FamilyId,
                peopleID = p.PeopleId,
                positionID = p.PositionInFamilyId,
                barcodeID = CmsData.Person.Barcode(CurrentDatabase, p.PeopleId)
			};

			Message response = new Message();
			response.setNoError();
			response.count = 1;
			response.data = SerializeJSON( results );

			return response;
		}

		private EntryPoint getCheckInEntryPointID()
		{
			EntryPoint checkInEntryPoint = (from e in CurrentDatabase.EntryPoints
														where e.Code == "CHECKIN"
														select e).FirstOrDefault();

			if( checkInEntryPoint != null ) {
				return checkInEntryPoint;
			} else {
				int maxEntryPointID = CurrentDatabase.EntryPoints.Max( e => e.Id );

				EntryPoint entry = new EntryPoint {
					Id = maxEntryPointID + 1,
					Code = "CHECKIN",
					Description = "Check-In",
					Hardwired = true
				};

				CurrentDatabase.EntryPoints.InsertOnSubmit( entry );
				CurrentDatabase.SubmitChanges();

				return entry;
			}
		}

        [HttpPost]
		public ActionResult PendingCheckIn( string data )
		{
			// Authenticate first
			if( !Auth() ) {
				return Message.createErrorReturn( "Authentication failed, please try again", Message.API_ERROR_INVALID_CREDENTIALS );
			}
            
            Message response = new Message();
			Message message = Message.createFromString( data );

            CheckInPending existing = CurrentDatabase.CheckInPendings.Where(c => c.PeopleId == CurrentDatabase.CurrentPeopleId).SingleOrDefault();

            if (existing != null)
            {
                existing.Stamp = DateTime.Now;
                existing.Data = message.data;
            }
            else
            {
                var pending = new CheckInPending
                {
                    Stamp = DateTime.Now,
                    Data = message.data,
                    PeopleId = CurrentDatabase.CurrentPeopleId,
                    FamilyId = CurrentDatabase.CurrentUserPerson.FamilyId
                };
                CurrentDatabase.CheckInPendings.InsertOnSubmit(pending);
            }
            CurrentDatabase.SubmitChanges();
            
            response.setNoError();
            response.count = 1;
            
            return response;
        }

        [HttpPost]
        public ActionResult GetPendingCheckIn(string data)
        {
            // Authenticate first
            if (!Auth())
            {
                return Message.createErrorReturn("Authentication failed, please try again", Message.API_ERROR_INVALID_CREDENTIALS);
            }

            Message message = Message.createFromString(data);

            CheckInPending pending = CurrentDatabase.CheckInPendings.Where(c => c.Id == message.id).SingleOrDefault();

            if (pending == null)
            {
                return Message.createErrorReturn("Pending check in not found", Message.API_ERROR_PENDING_CHECKIN_NOT_FOUND);
            }

            Message response = new Message();
            response.setNoError();
            response.count = 1;
            response.data = SerializeJSON(pending);

            return response;
        }

        [HttpPost]
        public ActionResult UpdatePendingCheckIn(string data)
        {
            // Authenticate first
            if (!Auth())
            {
                return Message.createErrorReturn("Authentication failed, please try again", Message.API_ERROR_INVALID_CREDENTIALS);
            }

            Message message = Message.createFromString(data);
            CheckInPending updated = JsonConvert.DeserializeObject<CheckInPending>(message.data);

            CheckInPending existing = CurrentDatabase.CheckInPendings.Where(c => c.Id == updated.Id).SingleOrDefault();

            if (existing == null)
            {
                return Message.createErrorReturn("Pending check in not found", Message.API_ERROR_PENDING_CHECKIN_NOT_FOUND);
            }

            existing.Stamp = updated.Stamp;
            existing.Data = updated.Data;

            CurrentDatabase.SubmitChanges();

            Message response = new Message();
            response.setNoError();
            response.count = 1;
            response.data = SerializeJSON(existing);

            return response;
        }

        [HttpPost]
		public ActionResult UpdateAttend( string data )
		{
			// Authenticate first
			if( !Auth() ) {
				return Message.createErrorReturn( "Authentication failed, please try again", Message.API_ERROR_INVALID_CREDENTIALS );
			}

			Message message = Message.createFromString( data );
			Message response = new Message();

			AttendanceBundle bundle = JsonConvert.DeserializeObject<AttendanceBundle>( message.data );
			bundle.recordAttendance( CurrentDatabase );

			if( message.device == Message.API_DEVICE_WEB ) {
				string bundleData = SerializeJSON( bundle );

				CheckInModel checkInModel = new CheckInModel(CurrentDatabase);
				checkInModel.SavePrintJob( message.kiosk, null, bundleData );

				response.setNoError();
				response.count = 1;
				response.data = bundleData;
			} else {
				response.setNoError();
				response.count = 1;
				response.data = SerializeJSON( bundle.createLabelData( CurrentDatabase ) );
			}

			return response;
		}

        [HttpPost]
        public ActionResult UpdateMembership(string data)
        {
            // Authenticate first
            if (!Auth())
            {
                return Message.createErrorReturn("Authentication failed, please try again", Message.API_ERROR_INVALID_CREDENTIALS);
            }

            Message message = Message.createFromString(data);
            Message response = new Message();

            OrgMembership membership = JsonConvert.DeserializeObject<OrgMembership>(message.data);

            OrganizationMember om = CurrentDatabase.OrganizationMembers.SingleOrDefault(m => m.PeopleId == membership.peopleID && m.OrganizationId == membership.orgID);

            if (om == null && membership.join)
            {
                om = OrganizationMember.InsertOrgMembers(CurrentDatabase, membership.orgID, membership.peopleID, MemberTypeCode.Member, DateTime.Today);
            }

            if (om != null && !membership.join)
            {
                om.Drop(CurrentDatabase, CurrentImageDatabase, DateTime.Now);

                DbUtil.LogActivity($"Dropped {om.PeopleId} for {om.Organization.OrganizationId} via checkin", peopleid: om.PeopleId, orgid: om.OrganizationId);
            }

            CurrentDatabase.SubmitChanges();

            // Check Entry Point and replace if Check-In
            CmsData.Person person = CurrentDatabase.People.FirstOrDefault(p => p.PeopleId == membership.peopleID);

            if (person?.EntryPoint != null && person.EntryPoint.Code == "CHECKIN" && om != null)
            {
                person.EntryPoint = om.Organization.EntryPoint;
                CurrentDatabase.SubmitChanges();
            }

            Guid barcode = CmsData.Person.Barcode(CurrentDatabase, membership.peopleID);
            response.data = SerializeJSON(barcode.ToString(), message.version);
            response.setNoError();
            response.count = 1;

            return response;
        }

        public ActionResult PrintJobs( string data )
		{
			if( !Auth() ) {
				return Message.createErrorReturn( "Authentication failed, please try again", Message.API_ERROR_INVALID_CREDENTIALS );
			}

			Message message = Message.createFromString( data );
			Message response = new Message();

			string[] kiosks = message.getArgStringAsArray( "," );

			List<PrintJob> printJobs = (from label in CurrentDatabase.PrintJobs
												where kiosks.Contains( label.Id )
                                                where label.JsonData != null
                                                where label.JsonData != ""
												select label).ToList();

			List<List<Label>> labels = new List<List<Label>>();

			foreach( PrintJob printJob in printJobs ) {
				AttendanceBundle attendanceBundle = JsonConvert.DeserializeObject<AttendanceBundle>( printJob.JsonData );
				attendanceBundle.labelSize = message.argInt;

				List<Label> labelGroup = attendanceBundle.createLabelData( CurrentDatabase );

				labels.Add( labelGroup );
                CurrentDatabase.PrintJobs.DeleteOnSubmit(printJob);
            }

			response.setNoError();
			response.count = labels.Count;
			response.data = SerializeJSON( labels );
            CurrentDatabase.SubmitChanges();

			return response;
		}

		[HttpPost]
		public ActionResult GroupSearch( string data )
		{
			if( !Auth() ) {
				return Message.createErrorReturn( "Authentication failed, please try again", Message.API_ERROR_INVALID_CREDENTIALS );
			}

			Message message = Message.createFromString( data );
			GroupSearch search = JsonConvert.DeserializeObject<GroupSearch>( message.data );

			CmsData.Person person = (from p in CurrentDatabase.People
											where p.PeopleId == search.peopleID
											select p).SingleOrDefault();

			if( person == null ) {
				return Message.createErrorReturn( "Person not found", Message.API_ERROR_PERSON_NOT_FOUND );
			}
			DbUtil.LogActivity( $"Check-In Group Search: {person.PeopleId}: {person.Name}" );

			List<Group> groups;

            try
            {
                using (SqlConnection db = new SqlConnection(Util.ConnectionString))
                {
                    groups = Group.forGroupFinder(db, person.BirthDate, search.campusID, search.dayID, search.showAll ? 1 : 0);
                }
            }
            catch (Exception e)
            {
                return Message.createErrorReturn(e.Message, Message.API_ERROR_PERSON_NOT_FOUND);
            }

            Message response = new Message();
			response.setNoError();
			response.data = SerializeJSON( groups );

			return response;
		}

		// Version for future API changes
		[SuppressMessage( "ReSharper", "UnusedParameter.Local" )]
		private static string SerializeJSON( object item, int version = 0 )
		{
			return JsonConvert.SerializeObject( item, new IsoDateTimeConverter {
				DateTimeFormat = "yyyy-MM-dd'T'HH:mm:ss"
			} );
		}
	}
}
