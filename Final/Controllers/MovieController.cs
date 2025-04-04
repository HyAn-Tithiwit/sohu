﻿using Final.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using PagedList;

namespace Final.Controllers
{
    public class MovieController : Controller
    {
        DataClassesDataContext db = new DataClassesDataContext();

        // GET: Movie/Index
        [HttpGet]
        public ActionResult Index(int? pageNowShowing, int? pageComingSoon)
        {
            DateTime today = DateTime.Today;

            var nowShowing = db.Movies
                .Where(m => m.ReleaseDate <= today)
                .OrderByDescending(m => m.ReleaseDate)
                .ToPagedList(pageNowShowing ?? 1, 4);

            var comingSoon = db.Movies
                .Where(m => m.ReleaseDate > today)
                .OrderBy(m => m.ReleaseDate)
                .ToPagedList(pageComingSoon ?? 1, 4);

            var firstMovie = nowShowing.FirstOrDefault();
            var trailerLink = firstMovie != null ? firstMovie.TrailerLink : "";

            var viewModel = new HomeIndexViewModel
            {
                NowShowing = nowShowing,
                ComingSoon = comingSoon,
                TrailerLink = trailerLink
            };

            return View(viewModel);
        }

        public ActionResult Show(string category, int? page)
        {
            int pageSize = 8;
            int pageNumber = (page ?? 1);
            IQueryable<Movy> movies;
            DateTime today = DateTime.Now;

            if (category == "NowShowing")
            {
                movies = db.Movies.Where(m => m.ReleaseDate <= today).OrderBy(m => m.Title);
            }
            else if (category == "ComingSoon")
            {
                movies = db.Movies.Where(m => m.ReleaseDate > today).OrderBy(m => m.Title);
            }
            else
            {
                return HttpNotFound();
            }

            ViewBag.Category = category;
            return View(movies.ToPagedList(pageNumber, pageSize));
        }

        [HttpGet]
        public JsonResult SearchMovies(string query)
        {
            var movies = db.Movies
                .Where(m => m.Title.Contains(query))
                .Select(m => new
                {
                    m.MovieID,
                    m.Title,
                    m.Image
                })
                .Take(5)
                .ToList();

            return Json(movies, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Detail(int id)
        {
            var movie = db.Movies
                .Where(m => m.MovieID == id)
                .Select(m => new MovieDetailViewModel
                {
                    MovieID = m.MovieID,
                    Title = m.Title,
                    Genre = m.Genre,
                    Duration = m.Duration ?? 0,
                    AgeRating = m.AgeRating,
                    Description = m.Description,
                    ReleaseDate = m.ReleaseDate,
                    Image = m.Image,
                    TrailerLink = m.TrailerLink,
                    Cinemas = db.Cinemas
                        .Where(c => db.Screenings.Any(s => s.MovieID == id && s.Room.RoomID != null && s.Room.CinemaID == c.CinemaID))
                        .Select(c => new CinemaViewModel
                        {
                            CinemaID = c.CinemaID,
                            Name = c.Name,
                            Address = c.Address
                        }).ToList(),
                    Foods = db.Foods
                        .Select(f => new FoodViewModel
                        {
                            FoodID = f.FoodID,
                            FoodName = f.FoodName,
                            Price = f.Price
                        }).ToList(),
                    TicketTypes = db.TicketTypes
                        .Select(t => new TicketTypeViewModel
                        {
                            TicketTypeID = t.TicketTypeID,
                            Name = t.TypeName,
                            Price = t.DefaultPrice
                        }).ToList()
                })
                .FirstOrDefault();

            if (movie == null)
            {
                return HttpNotFound();
            }

            return View(movie);
        }

        public ActionResult CinemaListPartial()
        {
            var cinemas = db.Cinemas.ToList();
            return PartialView(cinemas);
        }

        public ActionResult More(int id)
        {
            var movie = db.Movies
                .Where(m => m.MovieID == id)
                .Select(m => new MovieDetailViewModel
                {
                    MovieID = m.MovieID,
                    Title = m.Title,
                    Genre = m.Genre,
                    Duration = m.Duration ?? 0,
                    AgeRating = m.AgeRating,
                    Description = m.Description,
                    ReleaseDate = m.ReleaseDate,
                    Image = m.Image,
                    TrailerLink = m.TrailerLink,
                    Cinemas = db.Cinemas
                        .Where(c => db.Screenings.Any(s => s.MovieID == id && s.Room.RoomID != null && s.Room.CinemaID == c.CinemaID))
                        .Select(c => new CinemaViewModel
                        {
                            CinemaID = c.CinemaID,
                            Name = c.Name,
                            Address = c.Address
                        }).ToList(),
                    Foods = db.Foods
                        .Select(f => new FoodViewModel
                        {
                            FoodID = f.FoodID,
                            FoodName = f.FoodName,
                            Price = f.Price
                        }).ToList(),
                    TicketTypes = db.TicketTypes
                        .Select(t => new TicketTypeViewModel
                        {
                            TicketTypeID = t.TicketTypeID,
                            Name = t.TypeName,
                            Price = t.DefaultPrice
                        }).ToList()
                })
                .FirstOrDefault();

            if (movie == null)
            {
                return HttpNotFound();
            }

            return View(movie);
        }

        public JsonResult GetCinemas()
        {
            var cinemas = db.Cinemas
                .Select(c => new { c.CinemaID, c.Name })
                .ToList();
            return Json(cinemas, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetMoviesByCinema(int cinemaId)
        {
            var movies = db.Screenings
                .Where(s => s.Room.CinemaID == cinemaId)
                .Select(s => new { s.Movy.MovieID, s.Movy.Title })
                .Distinct()
                .ToList();
            return Json(movies, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetDatesByMovie(int movieId)
        {
            var dates = db.Screenings
                .Where(s => s.MovieID == movieId)
                .Select(s => s.StartTime.Date)
                .Distinct()
                .OrderBy(d => d)
                .ToList();
            return Json(dates.Select(d => d.ToString("yyyy-MM-dd")), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetShowtimesByDate(int movieId, string date)
        {
            DateTime selectedDate = DateTime.Parse(date);
            var showtimes = db.Screenings
                .Where(s => s.MovieID == movieId && s.StartTime.Date == selectedDate)
                .Select(s => s.StartTime.ToString("HH:mm"))
                .ToList();
            return Json(showtimes, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetScreenings(int cinemaId, int movieId)
        {
            var screenings = db.Screenings
                .Join(db.Rooms, s => s.RoomID, r => r.RoomID, (s, r) => new { s, r })
                .Where(sr => sr.s.MovieID == movieId && sr.r.CinemaID == cinemaId)
                .Select(sr => new
                {
                    sr.s.ScreeningID,
                    sr.s.StartTime  // Retrieve StartTime as DateTime
                })
                .ToList()  // Execute the query and load data into memory
                .Select(sr => new
                {
                    ScreeningID = sr.ScreeningID,
                    StartTime = sr.StartTime.ToString("HH:mm")  // Format in C# after query execution
                })
                .ToList();

            return Json(screenings, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetSeats(int screeningId)
        {
            var screening = db.Screenings.FirstOrDefault(sc => sc.ScreeningID == screeningId);
            if (screening == null)
            {
                return Json(new { error = "Screening not found" }, JsonRequestBehavior.AllowGet);
            }

            var seats = db.Seats
                .Where(s => s.RoomID == screening.RoomID)
                .Select(s => new
                {
                    s.SeatID,
                    s.SeatNumber,
                    s.RowNumber,
                    s.ColumnNumber,
                    s.Status
                })
                .ToList();

            return Json(seats, JsonRequestBehavior.AllowGet);
        }

        // Updated: Fix seat status handling
        public JsonResult GetSeatsMatrix(int screeningId)
        {
            var screening = db.Screenings.FirstOrDefault(scr => scr.ScreeningID == screeningId);
            if (screening == null)
            {
                return Json(new { error = "Screening not found" }, JsonRequestBehavior.AllowGet);
            }

            // Pre-fetch booked seats from OrderDetails
            var bookedSeats = db.OrderDetails
                .Where(od => od.ScreeningID == screeningId)
                .Select(od => od.SeatID)
                .ToList();

            // Pre-fetch active temporary reservations
            var reservedSeats = db.TemporaryReservations
                .Where(tr => tr.ScreeningID == screeningId && tr.ExpirationTime > DateTime.Now)
                .Select(tr => tr.SeatID)
                .ToList();

            // Fetch seats with updated status
            var seats = db.Seats
                .Where(s => s.RoomID == screening.RoomID)
                .Select(s => new
                {
                    s.SeatID,
                    s.SeatNumber,
                    s.RowNumber,
                    s.ColumnNumber,
                    Status = bookedSeats.Contains(s.SeatID) ? "Sold" :
                             reservedSeats.Contains(s.SeatID) ? "Reserved" : "Available"
                })
                .ToList();

            return Json(seats, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetTicketTypes()
        {
            var ticketTypes = db.TicketTypes
                .Select(t => new
                {
                    t.TicketTypeID,
                    t.TypeName,
                    t.DefaultPrice
                }).ToList();

            return Json(ticketTypes, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult ConfirmBooking(int screeningId, List<int> seats, List<FoodOrder> foods, int? ticketTypeId, int? customerId)
        {
            seats = seats ?? new List<int>();
            foods = foods ?? new List<FoodOrder>();

            // Log input data
            Console.WriteLine($"Input - ScreeningID: {screeningId}, Seats: [{string.Join(", ", seats)}], Foods: {foods.Count}, TicketTypeId: {ticketTypeId}, CustomerId: {customerId}");
            foreach (var food in foods)
            {
                Console.WriteLine($"Food Item - FoodId: {food.FoodId}, Quantity: {food.Quantity}");
            }

            if (!ticketTypeId.HasValue)
            {
                Console.WriteLine("Validation failed: TicketTypeId is null");
                return Json(new { success = false, message = "Loại vé không được xác định!" });
            }

            var screening = db.Screenings.FirstOrDefault(s => s.ScreeningID == screeningId);
            if (screening == null)
            {
                Console.WriteLine("Validation failed: Screening not found");
                return Json(new { success = false, message = "Không tìm thấy suất chiếu!" });
            }

            var room = db.Rooms.FirstOrDefault(r => r.RoomID == screening.RoomID);
            var cinema = db.Cinemas.FirstOrDefault(c => c.CinemaID == room.CinemaID);
            var movie = db.Movies.FirstOrDefault(m => m.MovieID == screening.MovieID);
            if (room == null || cinema == null || movie == null)
            {
                Console.WriteLine("Validation failed: Room, Cinema, or Movie data invalid");
                return Json(new { success = false, message = "Dữ liệu phim/rạp/phòng không hợp lệ!" });
            }

            var bookedSeats = db.OrderDetails
                .Where(od => od.ScreeningID == screeningId && seats.Contains((int)od.SeatID))
                .Select(od => od.SeatID)
                .ToList();

            var reservedSeats = db.TemporaryReservations
                .Where(tr => tr.ScreeningID == screeningId && tr.ExpirationTime > DateTime.Now && seats.Contains((int)tr.SeatID))
                .Select(tr => tr.SeatID)
                .ToList();

            var unavailableSeats = bookedSeats.Union(reservedSeats).Distinct().ToList();
            if (unavailableSeats.Any())
            {
                Console.WriteLine($"Validation failed: Unavailable seats - {string.Join(", ", unavailableSeats)}");
                return Json(new { success = false, message = "Một số ghế đã được đặt hoặc đang được giữ: " + string.Join(", ", unavailableSeats) });
            }

            string sessionId = (customerId == null || customerId == 0) ? Session.SessionID : null;
            Console.WriteLine($"SessionID: {sessionId}");

            if (db.Connection.State == System.Data.ConnectionState.Closed)
            {
                db.Connection.Open();
                Console.WriteLine("Database connection opened");
            }

            using (var transaction = db.Connection.BeginTransaction())
            {
                try
                {
                    db.Transaction = transaction;
                    DateTime now = DateTime.Now;
                    DateTime expirationTime = now.AddMinutes(5);
                    var ticketType = db.TicketTypes.FirstOrDefault(tt => tt.TicketTypeID == ticketTypeId.Value);
                    if (ticketType == null)
                    {
                        Console.WriteLine("Validation failed: TicketType not found");
                        return Json(new { success = false, message = "Loại vé không hợp lệ!" });
                    }

                    if (customerId != null && customerId != 0)
                    {
                        Console.WriteLine("Processing logged-in user order");
                        var order = new Order
                        {
                            CustomerID = customerId.Value,
                            ScreeningID = screeningId,
                            OrderDate = now,
                            PaymentStatus = "Completed",
                            TotalAmount = 0
                        };
                        db.Orders.InsertOnSubmit(order);
                        db.SubmitChanges();
                        Console.WriteLine($"Order created - OrderID: {order.OrderID}");

                        foreach (var seatId in seats)
                        {
                            var seat = db.Seats.FirstOrDefault(s => s.SeatID == seatId);
                            db.OrderDetails.InsertOnSubmit(new OrderDetail
                            {
                                OrderID = order.OrderID,
                                ItemType = "Ticket",
                                SeatID = seatId,
                                SeatNumber = seat?.SeatNumber,
                                TicketTypeID = ticketTypeId.Value,
                                TicketTypeName = ticketType.TypeName,
                                Price = ticketType.DefaultPrice,
                                Quantity = 1,
                                ScreeningID = screeningId,
                                ScreeningTime = screening.StartTime,
                                MovieID = movie.MovieID,
                                MovieTitle = movie.Title,
                                CinemaID = cinema.CinemaID,
                                CinemaName = cinema.Name
                            });
                            Console.WriteLine($"Queued ticket - SeatID: {seatId}");
                        }

                        // Save each food item individually (no grouping)
                        foreach (var food in foods)
                        {
                            var foodItem = db.Foods.FirstOrDefault(f => f.FoodID == food.FoodId);
                            if (foodItem == null)
                            {
                                Console.WriteLine($"Food not found - FoodID: {food.FoodId}");
                                transaction.Rollback();
                                return Json(new { success = false, message = $"Không tìm thấy món ăn ID: {food.FoodId}" });
                            }
                            db.OrderDetails.InsertOnSubmit(new OrderDetail
                            {
                                OrderID = order.OrderID,
                                ItemType = "Food",
                                FoodID = foodItem.FoodID,
                                FoodName = foodItem.FoodName,
                                FoodQuantity = food.Quantity,
                                Price = foodItem.Price * food.Quantity,
                                Quantity = 1,
                                ScreeningID = screeningId,
                                MovieID = movie.MovieID,
                                MovieTitle = movie.Title,
                                CinemaID = cinema.CinemaID,
                                CinemaName = cinema.Name
                            });
                            Console.WriteLine($"Queued food - FoodID: {food.FoodId}, Quantity: {food.Quantity}");
                        }

                        db.SubmitChanges();
                        Console.WriteLine("Changes submitted for OrderDetails");

                        order.TotalAmount = db.OrderDetails
                            .Where(od => od.OrderID == order.OrderID)
                            .Sum(od => od.Price);
                        db.SubmitChanges();
                        Console.WriteLine($"Updated TotalAmount: {order.TotalAmount}");

                        transaction.Commit();
                        Console.WriteLine("Transaction committed for logged-in user");
                        return Json(new { success = true, message = "Đặt vé thành công! Đơn hàng đã được tạo." });
                    }
                    else
                    {
                        Console.WriteLine("Processing guest user temporary reservation");
                        foreach (var seatId in seats)
                        {
                            var seat = db.Seats.FirstOrDefault(s => s.SeatID == seatId);
                            db.TemporaryReservations.InsertOnSubmit(new TemporaryReservation
                            {
                                SessionID = sessionId,
                                ScreeningID = screeningId,
                                SeatID = seatId,
                                SeatNumber = seat?.SeatNumber,
                                TicketTypeID = ticketTypeId.Value,
                                TicketTypeName = ticketType.TypeName,
                                Price = ticketType.DefaultPrice,
                                ItemType = "Ticket",
                                Quantity = 1,
                                ScreeningTime = screening.StartTime,
                                ExpirationTime = expirationTime,
                                MovieID = movie.MovieID,
                                MovieTitle = movie.Title,
                                CinemaID = cinema.CinemaID,
                                CinemaName = cinema.Name
                            });
                            Console.WriteLine($"Queued ticket - SeatID: {seatId}");
                        }

                        // Save each food item individually (no grouping)
                        foreach (var food in foods)
                        {
                            var foodItem = db.Foods.FirstOrDefault(f => f.FoodID == food.FoodId);
                            if (foodItem == null)
                            {
                                Console.WriteLine($"Food not found - FoodID: {food.FoodId}");
                                transaction.Rollback();
                                return Json(new { success = false, message = $"Không tìm thấy món ăn ID: {food.FoodId}" });
                            }
                            db.TemporaryReservations.InsertOnSubmit(new TemporaryReservation
                            {
                                SessionID = sessionId,
                                ScreeningID = screeningId,
                                FoodID = foodItem.FoodID,
                                FoodName = foodItem.FoodName,
                                FoodQuantity = food.Quantity,
                                Price = foodItem.Price * food.Quantity,
                                ItemType = "Food",
                                Quantity = 1,
                                ScreeningTime = screening.StartTime,
                                ExpirationTime = expirationTime,
                                MovieID = movie.MovieID,
                                MovieTitle = movie.Title,
                                CinemaID = cinema.CinemaID,
                                CinemaName = cinema.Name,
                                SeatID = null  // Use 0 if NOT NULL
                            });
                            Console.WriteLine($"Queued food - FoodID: {food.FoodId}, Quantity: {food.Quantity}");
                        }

                        db.SubmitChanges();
                        Console.WriteLine("Changes submitted for TemporaryReservations");

                        transaction.Commit();
                        Console.WriteLine("Transaction committed for guest user");
                        return Json(new { success = true, message = "Chọn vé đã được lưu vào giỏ hàng tạm thời (5 phút)!" });
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception: {ex.Message}");
                    Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                    if (ex.InnerException != null)
                    {
                        Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                    }
                    transaction.Rollback();
                    return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
                }
                finally
                {
                    db.Connection.Close();
                    Console.WriteLine("Database connection closed");
                }
            }
        }

        // Ensure FoodOrder class is defined
        public class FoodOrder
        {
            public int FoodId { get; set; }
            public int Quantity { get; set; }
        }
    }
}