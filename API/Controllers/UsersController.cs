using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Interfaces;
using AutoMapper;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Authorize]
public class UsersController(IUserRepository userRepository, IMapper mapper, IPhotoService photoService) : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<MemberDto>>> GetUsersAsync()
    {
        var users = await userRepository.GetMembersAsync();

        return Ok(users);
    }

    [HttpGet("{username}")]
    public async Task<ActionResult<MemberDto>> GetUser(string username)
    {
        var user = await userRepository.GetMemberAsync(username);

        if (user == null)
        {
            return NotFound();
        }

        return user;
    }

    [HttpPut]
    public async Task<ActionResult> UpdateUserAsync(MemberUpdateDto memberUpdateDto)
    {
        AppUser? user = await userRepository.GetUserByUsernameAsync(User.GetUsername());

        if (user == null)
        {
            return BadRequest("Could not find user");
        }

        mapper.Map(memberUpdateDto, user);

        // userRepository.Update(user); // to explicitly say to EF that the object is updated even if there is no change

        if (await userRepository.SaveAllAsync())
        {
            return NoContent();
        }

        return BadRequest("Failed to update user");
    }

    [HttpPost("add-photo")]
    public async Task<ActionResult<PhotoDto>> AddPhotoAsync(IFormFile file)
    {
        AppUser? user = await userRepository.GetUserByUsernameAsync(User.GetUsername());

        if (user == null)
        {
            return BadRequest("Cannot update user");
        }

        ImageUploadResult result = await photoService.AddPhotoAsync(file);

        if (result.Error != null)
        {
            return BadRequest(result.Error.Message);
        }

        Photo photo = new Photo
        {
            Url = result.SecureUrl.AbsoluteUri,
            PublicId = result.PublicId
        };

        user.Photos.Add(photo);

        if (await userRepository.SaveAllAsync())
        {
            return CreatedAtAction(nameof(GetUser), new { username = user.UserName }, mapper.Map<PhotoDto>(photo));
        }

        return BadRequest("Problem adding photo");
    }

    [HttpPut("set-main-photo/{photoId:int}")]
    public async Task<ActionResult> SetMainPhotoAsync(int photoId)
    {
        AppUser? user = await userRepository.GetUserByUsernameAsync(User.GetUsername());
        if (user == null)
        {
            return BadRequest("Could not find user");
        }

        Photo? photo = user.Photos.FirstOrDefault(x => x.Id == photoId);
        if (photo == null)
        {
            return NotFound();
        }
        if (photo.IsMain)
        {
            return BadRequest("This is already your main photo");
        }

        Photo? currentMain = user.Photos.FirstOrDefault(x => x.IsMain);
        if (currentMain != null)
        {
            currentMain.IsMain = false;
        }

        photo.IsMain = true;
        if (await userRepository.SaveAllAsync())
        {
            return NoContent();
        }

        return BadRequest("Problem setting main photo");
    }
}
